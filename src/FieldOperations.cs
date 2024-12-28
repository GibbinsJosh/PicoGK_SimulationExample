using System.Numerics;
using PicoGK;

namespace Leap71.Simulation
{
    public static class FieldOperations
    {
        /// <summary>
        /// Approximates the gradient of a scalar field at a given position using finite differences.
        /// </summary>
        /// <param name="field">The scalar field to compute the gradient on.</param>
        /// <param name="position">The position to compute the gradient (in mm).</param>
        /// <param name="step">Step size for finite difference approximation (default: 1e-3).</param>
        /// <returns>The gradient vector as a Vector3.</returns>
        public static Vector3 ComputeGradient(ScalarField field, Vector3 position, float step = 1e-3f)
        {
            float dTdx = ComputeFiniteDifference(field, position, new Vector3(step, 0, 0));
            float dTdy = ComputeFiniteDifference(field, position, new Vector3(0, step, 0));
            float dTdz = ComputeFiniteDifference(field, position, new Vector3(0, 0, step));

            return new Vector3(dTdx, dTdy, dTdz);
        }

        /// <summary>
        /// Approximates the Laplacian of a scalar field at a given position using finite differences.
        /// </summary>
        /// <param name="field">The scalar field to compute the Laplacian on.</param>
        /// <param name="position">The position to compute the Laplacian (in mm).</param>
        /// <param name="step">Step size for finite difference approximation (default: 1e-3).</param>
        /// <returns>The Laplacian value as a float.</returns>
        public static float ComputeLaplacian(ScalarField field, Vector3 position, float step = 1e-3f)
        {
            float T = GetValueOrThrow(field, position);
            float Txx = ComputeSecondOrderFiniteDifference(field, position, new Vector3(step, 0, 0), T);
            float Tyy = ComputeSecondOrderFiniteDifference(field, position, new Vector3(0, step, 0), T);
            float Tzz = ComputeSecondOrderFiniteDifference(field, position, new Vector3(0, 0, step), T);

            return Txx + Tyy + Tzz;
        }

        /// <summary>
        /// Retrieves the vector value of a vector field at a given position.
        /// </summary>
        /// <param name="field">The vector field to query.</param>
        /// <param name="position">The position to query (in mm).</param>
        /// <returns>The vector value as a Vector3.</returns>
        public static Vector3 GetVector(VectorField field, Vector3 position)
        {
            if (field.bGetValue(position, out Vector3 value))
            {
                return value;
            }
            throw new Exception($"No vector value found at position {position}.");
        }

        /// <summary>
        /// Helper method to compute a first-order finite difference for gradient approximation.
        /// </summary>
        private static float ComputeFiniteDifference(ScalarField field, Vector3 position, Vector3 offset)
        {
            float forward = GetValueOrThrow(field, position + offset);
            float backward = GetValueOrThrow(field, position - offset);
            return (forward - backward) / (2 * offset.Length());
        }

        /// <summary>
        /// Helper method to compute a second-order finite difference for Laplacian approximation.
        /// </summary>
        private static float ComputeSecondOrderFiniteDifference(ScalarField field, Vector3 position, Vector3 offset, float centerValue)
        {
            float forward = GetValueOrThrow(field, position + offset);
            float backward = GetValueOrThrow(field, position - offset);
            return (forward - 2 * centerValue + backward) / (offset.LengthSquared());
        }

        /// <summary>
        /// Retrieves the value of a scalar field or throws an exception if the position is inactive.
        /// </summary>
        private static float GetValueOrThrow(ScalarField field, Vector3 position)
        {
            if (field.bGetValue(position, out float value))
            {
                return value;
            }
            throw new Exception($"No scalar value found at position {position}.");
        }
    }
}