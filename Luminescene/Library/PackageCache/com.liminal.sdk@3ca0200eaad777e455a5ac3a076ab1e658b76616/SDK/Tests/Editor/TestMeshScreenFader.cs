using Liminal.Core.Fader;
using NUnit.Framework;
using UnityEngine;

namespace Liminal.SDK.Test
{
    [TestFixture]
    public class MeshScreenFaderTest
    {
        [Test]
        public void CanBeInstantiated()
        {
            var go = new GameObject();
            var fader = go.AddComponent<MeshScreenFader>();
            fader.runInEditMode = true;

            Assert.That(fader, Is.Not.Null);
        }

        [Test]
        public void CanFindShader()
        {
            var shader = Shader.Find(MeshScreenFader.ShaderName);

            Assert.That(shader, Is.Not.Null);
        }
    }
}