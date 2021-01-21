namespace Liminal.SDK.Examples
{
    using System.Collections;
    using UnityEngine;
    using Core;

    /// <summary>
    /// A controller for the spinning cube example scene, containing the exit point to the application.
    /// </summary>
    public class SpinningCubeExample
        : MonoBehaviour
    {
        public float Speed = 5;

        private void Update()
        {
            transform.Rotate(Vector3.up, Time.deltaTime*Speed);
        }

        private void OnEnable()
        {
            StartCoroutine(ShutDown());
        }

        private IEnumerator ShutDown()
        {
            yield return new WaitForSeconds(100);
            End();
        }

        public void End()
        {
            ExperienceApp.End();
        }
    }
}
