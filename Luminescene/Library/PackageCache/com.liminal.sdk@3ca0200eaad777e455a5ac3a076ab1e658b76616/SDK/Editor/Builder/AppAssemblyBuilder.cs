using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Liminal.Cecil.Mono.Cecil;
using Liminal.Cecil.Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Liminal.SDK.Editor.Build
{
    /// <summary>
    /// Builds and processes an Experience App assembly.
    /// </summary>
    internal class AppAssemblyBuilder
    {
        internal class AssemblyBuildInfo
        {
            public string Name;
            public BuildTarget BuildTarget;
            public BuildTargetGroup BuildTargetGroup;
            public int Version;
        }

        /// <summary>
        /// Builds the application assembly with the specificed Assembly name, and writes it to the supplied output path.
        /// </summary>
        /// <param name="buildInfo">Assembly build information.</param>
        /// <param name="outputPath">The output path of the assembly file.</param>
        public void Build(AssemblyBuildInfo buildInfo, string outputPath)
        {
            // Build the assembly from the (non-editor) CS scripts in the project
            if (BuildAssembly(buildInfo, outputPath))
            {
                PostProcessAssembly(buildInfo, outputPath);
            }
        }

        private bool BuildAssembly(AssemblyBuildInfo buildInfo, string outputPath)
        {
            // Fetch all the non-editor scripts in the project that should be compiled
            // into the final assembly. The output is essentially the same as Assembly-CSharp.
            var scripts = GetAssemblyScripts();
            if (scripts.Length == 0)
            {
                // No scripts to compile
                // Returning false here indicates that no assembly is required
                return false;
            }

            var buildSuccess = false;

            var json = File.ReadAllText(BuildWindowConsts.BuildWindowConfigPath);
            var config = JsonUtility.FromJson<BuildWindowConfig>(json);

            var configAdditionalReferences = config.DefaultAdditionalReferences;
            configAdditionalReferences.AddRange(config.AdditionalReferences);

            foreach (var additionalReference in configAdditionalReferences)
            {
                Debug.Log(additionalReference);
            }

            var additionalReferences = new string[configAdditionalReferences.Count];
            for (var i = 0; i < additionalReferences.Length; i++)
            {
                additionalReferences[i] = GetAssemblyPath(configAdditionalReferences[i]);
            }

            var builder = new AssemblyBuilder(outputPath, scripts)
            {
                buildTargetGroup = buildInfo.BuildTargetGroup,
                buildTarget = buildInfo.BuildTarget,
                additionalReferences = additionalReferences,
            };

            // Hook in listeners
            builder.buildStarted += (path) => Debug.LogFormat("[Liminal.Build] Assembly build started: {0}", buildInfo.Name);
            builder.buildFinished += (path, messages) =>
            {
                var warnings = messages.Where(m => m.type == CompilerMessageType.Warning);
                var warningCount = warnings.Count();

                var errors = messages.Where(m => m.type == CompilerMessageType.Error);
                var errorCount = errors.Count();

                Debug.LogFormat("[Liminal.Build] Assembly build finished. Warnings: {0} - Errors: {1} - Output: {2}", warningCount, errorCount, path);
                
                foreach (var m in warnings)
                {
                    Debug.LogWarning(FormatCompilerMessage(m));
                }

                foreach (var m in errors)
                {
                    Debug.LogError(FormatCompilerMessage(m));
                }

                buildSuccess = (errorCount == 0);
            };

            // Run the build and wait for it to finish
            // Build is run on a separate thread, so we need to wait for its status to change
            builder.Build();
            while (builder.status != AssemblyBuilderStatus.Finished)
                System.Threading.Thread.Sleep(10);

            // Throw if the build failed
            if (!buildSuccess)
                throw new Exception("Assembly build failed");

            return true;
        }

        private void PostProcessAssembly(AssemblyBuildInfo buildInfo, string outputPath)
        {
            var asmName = buildInfo.Name;
            var readerParameters = new ReaderParameters()
            {
                InMemory = true,
                ReadSymbols = true
            };
            var asmDef = AssemblyDefinition.ReadAssembly(outputPath, readerParameters);
            asmDef.Name.Name = asmName;
            asmDef.Name.Version = new Version(0, buildInfo.Version);
            asmDef.MainModule.Name = asmName + ".dll";

            // Change all assembly references within the main module to point to the new assembly name
            foreach (var asmRef in asmDef.MainModule.AssemblyReferences)
            {
                if (asmRef.Name == BuildConsts.ProjectAssemblyName)
                    asmRef.Name = asmName;
            }

            var methods = GetAllMethodDefinitions(asmDef);

            // Warn if there is no call to ExperienceApp::End() found anywhere in the assembly
            if (!FindExperienceAppEndCall(asmDef, methods))
                throw new Exception("No method call to Liminal.SDK.Core.ExperienceApp::End() was found. Your app will never end. You app will not be approved with a call to End().");

            // Replace UnityEngine.Object::Instantiate method calls with LiminalObject::Instantiate
            // This is required to overcome serialization limitations within Unity when using code from an assembly
            // loaded into an AppDomain at runtime
            ReplaceInstantiateMethods(asmDef, methods);

            // Write assembly back to disk
            //using (var ms = new MemoryStream())
            //{
            //    asmDef.Write(ms, new WriterParameters() { WriteSymbols = true });
            //    //File.WriteAllBytes(outputPath + ".foo", ms.ToArray());
            //    File.WriteAllBytes(outputPath, ms.ToArray());
            //}

            // CJS: This `should` be the same as the block above?!?
            asmDef.Write(outputPath, new WriterParameters(){WriteSymbols = true});
            //asmDef.Write(outputPath + ".foo", new WriterParameters(){WriteSymbols = true});
        }
        
        private bool FindExperienceAppEndCall(AssemblyDefinition asmDef, IEnumerable<MethodDefinition> methods)
        {
            TypeReference appTypeRef;
            asmDef.MainModule.TryGetTypeReference("Liminal.SDK.Core.ExperienceApp", out appTypeRef);

            // Can't possibly contain the call if the type reference doesn't exist?!
            if (appTypeRef == null)
                return false;

            foreach (var methodDef in methods)
            {
                if (!methodDef.HasBody)
                    continue;

                // Finf all calls in the method bod

                var methodCalls = methodDef.Body.Instructions
                    .Where(x => x.OpCode == OpCodes.Call)
                    .ToArray();

                foreach (var instruction in methodCalls)
                {
                    var mRef = instruction.Operand as MethodReference;
                    if (mRef == null)
                        continue;
                    
                    // Is this a call to ExperienceApp::End? If so, we can bail out with success
                    if (mRef.Name == "End" && mRef.DeclaringType == appTypeRef)
                        return true;
                }
            }

            // No method calls to ExperienceApp::End were found, exit with failure
            return false;
        }

        private void ReplaceInstantiateMethods(AssemblyDefinition asmDef, IEnumerable<MethodDefinition> methods)
        {
            // Get a TypeReference to the UnityEngine.Object type
            // This is where all the Institate() method defintions are stored
            TypeReference unityObjectTypeRef;
            asmDef.MainModule.TryGetTypeReference("UnityEngine.Object", out unityObjectTypeRef);

            // Ensure that the LiminalObject type is imported into the module
            // This returns a type reference for us that we will override the UnityEngine.Object calls with
            var liminalObjectTypeRef = asmDef.MainModule.ImportReference(typeof(LiminalObject));

            // For every declared method in the assembly, replace any UnityEngine.Object::Instantiate with LiminalObject::Instantiate
            foreach (var methodDef in methods)
            {
                ReplaceInstantiateCallsInMethod(methodDef, unityObjectTypeRef, liminalObjectTypeRef);
            }
        }

        private void ReplaceInstantiateCallsInMethod(MethodDefinition targetMethod, TypeReference unityTypeRef, TypeReference replacementTypeRef)
        {
            if (!targetMethod.HasBody)
                return;

            // Find all calls within the method body
            var methodCalls = targetMethod.Body.Instructions
                .Where(x => x.OpCode == OpCodes.Call)
                .ToArray();
            
            foreach (var instruction in methodCalls)
            {
                var mRef = instruction.Operand as MethodReference;
                if (mRef == null)
                    continue;

                // We want to replace any UnityEngine.Object::Instantiate method calls (ALL overloads) with their equivalent LiminalObject::Instantiate calls
                // If the method name is "Instaniate" and is declared by the UnityEngine.Object type, we should replace the operand
                if (mRef.Name == "Instantiate" && mRef.DeclaringType == unityTypeRef)
                {
                    instruction.Operand = CloneMethodWithDeclaringType(mRef, replacementTypeRef);
                }
            }
        }

        private MethodReference CloneMethodWithDeclaringType(MethodReference methodRef, TypeReference declaringTypeRef)
        {
            // If the input method reference is generic, it will be wrapped in a GenericInstanceMethod object
            var genericRef = methodRef as GenericInstanceMethod;
            if (genericRef != null)
            {
                // The actual method data we need to replicate is the ElementMethod
                // Replace the method reference with the element method from the generic wrapper
                methodRef = methodRef.GetElementMethod();
            }

            // Build a new method reference that matches the original exactly, but with a different declaring type
            var newRef = new MethodReference(methodRef.Name, methodRef.ReturnType, declaringTypeRef)
            {
                CallingConvention = methodRef.CallingConvention,
                HasThis = methodRef.HasThis,
                ExplicitThis = methodRef.ExplicitThis,
                MethodReturnType = methodRef.MethodReturnType,
            };

            // Clone method input parameters
            foreach (var p in methodRef.Parameters)
            {
                newRef.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
            }

            // Clone method generic parameters
            foreach (var p in methodRef.GenericParameters)
            {
                newRef.GenericParameters.Add(new GenericParameter(p.Name, newRef));
            }

            if (genericRef == null)
            {
                // For non-generic methods, we can simply return the new method reference
                return newRef;
            }
            else
            {
                // For generic methods, copy the generic arguments into the new method refernce
                var newGenericRef = new GenericInstanceMethod(newRef);
                foreach (var typeDef in genericRef.GenericArguments)
                {
                    newGenericRef.GenericArguments.Add(typeDef);
                }

                // Done
                return newGenericRef;
            }
        }

        #region Utilities

        private string[] GetAssemblyScripts()
        {
            // This section is used to build the AppModule.dll so we only want scripts from the project that the Platform will not include.

            var list = new List<string>();
            var csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            foreach (var csFilePath in csFiles)
            {
                var relativePath = csFilePath.Substring(Application.dataPath.Length).Replace("\\", "/");

                // Ignore any scripts under Editor/ folders
                if (relativePath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                if (relativePath.IndexOf($"/{BuildWindowConsts.PlatformViewerFolderName}/", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                if (relativePath.IndexOf("/Oculus/VR/Scripts", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                if (relativePath.IndexOf("/VR/Devices", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                if (relativePath.IndexOf("Frameworks/SteamVR", StringComparison.OrdinalIgnoreCase) > -1)
                    continue;

                var path = "Assets" + relativePath;
                list.Add(path);
            }

            return list.ToArray();
        }

        private IEnumerable<MethodDefinition> GetAllMethodDefinitions(AssemblyDefinition asmDef)
        {
            return asmDef.Modules
                    .SelectMany(m => m.Types
                        .Concat(m.Types.SelectMany(x => x.NestedTypes))
                        .SelectMany(x => x.Methods)
                    );
        }

        private string FormatCompilerMessage(CompilerMessage message)
        {
            return string.Format("{0}\nin {1}, Line {2}, Column {3}", message.message, message.file, message.line, message.column);
        }

        private string GetAssemblyPath(string name)
        {
            var asm = System.Reflection.Assembly.Load(name);
            return asm.Location;
        }

        #endregion
    }
}
