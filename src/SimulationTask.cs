//
// SPDX-License-Identifier: CC0-1.0
//
// This example code file is released to the public under Creative Commons CC0.
// See https://creativecommons.org/publicdomain/zero/1.0/legalcode
//
// To the extent possible under law, LEAP 71 has waived all copyright and
// related or neighboring rights to this PicoGK example code file.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//


using System.Numerics;
using PicoGK;

namespace Leap71
{
    using ShapeKernel;

    namespace Simulation
    {
        public class SimulationSetup
        {
            public static void WriteTask()
            {
                // physical inputs
                float fFluidDensity         = 1000f;            // kg/m3
                float fFluidViscosity       = 0.00000897f;      // m2/s
                float fFluidInletVelocity   = 1.5f;             // m/s
                float fFluidInitialTemp     = 300f;             // K
                float fSolidInitialTemp     = 350f;             // K

                // geometric inputs
                SimpleFlowDevice oPipe      = new SimpleFlowDevice();
                Voxels voxFluidDomain       = oPipe.voxGetFluidDomain();
                Voxels voxSolidDomain       = oPipe.voxGetSolidDomain();
                Voxels voxInletPatch        = oPipe.voxGetInletPatch();
                ScalarField oFluidTempField = oPipe.oGetFluidTemperatureField();
                ScalarField oSolidTempField = oPipe.oGetSolidTemperatureField();

                // create VDB file from input data
                string strVDBFilePath       = Sh.strGetExportPath(Sh.EExport.VDB, "SimpleFluidSimulation");
                SimpleFluidSimulationOutput oOutput = new(  strVDBFilePath,
                                                            fFluidDensity,
                                                            fFluidViscosity,
                                                            fFluidInletVelocity,
                                                            voxFluidDomain,
                                                            voxSolidDomain,
                                                            voxInletPatch,
                                                            oFluidTempField,
                                                            oSolidTempField);
                
                Library.Log("Finished Task.");
                return;
            }

            public static void ReadTask()
            {
                // load VDB file and retrieve simulation inputs
                string strVDBFilePath                   = Sh.strGetExportPath(Sh.EExport.VDB, "SimpleFluidSimulation");
                SimpleFluidSimulationInput oData        = new(strVDBFilePath);

                // get data
                Voxels voxFluidDomain                   = oData.voxGetFluidDomain();
                Voxels voxSolidDomain                   = oData.voxGetSolidDomain();
                ScalarField oDensityField               = oData.oGetDensityField();
                ScalarField oViscosityField             = oData.oGetViscosityField();
                VectorField oVelocityField              = oData.oGetVelocityField();
                ScalarField oFluidTempField             = oData.oGetTemperatureField("fluid");
                ScalarField oSolidTempField             = oData.oGetTemperatureField("solid");

                // Perform heat transfer simulation
                SimulateHeatTransfer(
                    oFluidTempField,
                    oSolidTempField,
                    oVelocityField,
                    voxFluidDomain,             // Pass fluid domain
                    voxSolidDomain,             // Pass solid domain
                    fDensity: 1000f,            // kg/m3
                    fSpecificHeat: 4200f,       // J/(kg*K)
                    fThermalConductivity: 0.6f, // W/(m*K)
                    fTimeStep: 0.01f,           // s
                    nIterations: 100            // Number of iterations
                );

                // get bounding box and probe fluid domain values
                // use your own resolution / step length
                BBox3 oBBox                     = Sh.oGetBoundingBox(voxFluidDomain);
                float fStep                     = 2f;
                for (float fZ = oBBox.vecMin.Z; fZ <= oBBox.vecMax.Z; fZ += fStep)
                {
                    for (float fX = oBBox.vecMin.X; fX <= oBBox.vecMax.X; fX += fStep)
                    {
                        for (float fY = oBBox.vecMin.Y; fY <= oBBox.vecMax.Y; fY += fStep)
                        {
                            Vector3 vecPosition = new Vector3(fX, fY, fZ);

                            // query density
                            bool bSuccess = oDensityField.bGetValue(vecPosition, out float fFieldValue);
                            if (bSuccess == true)
                            {
                                float fDensityValue = fFieldValue;
                                // todo: do something with the value...
                            }

                            // query viscosity
                            bSuccess = oViscosityField.bGetValue(vecPosition, out fFieldValue);
                            if (bSuccess == true)
                            {
                                float fViscosity = fFieldValue;
                                // todo: do something with the value...
                            }

                            // query velocity
                            bSuccess = oVelocityField.bGetValue(vecPosition, out Vector3 vecFieldValue);
                            if (bSuccess == true)
                            {
                                Vector3 vecVelocity = vecFieldValue;
                                // todo: do something with the value...
                            }

                            // query fluid temperature
                            bSuccess = oFluidTempField.bGetValue(vecPosition, out fFieldValue);
                            if (bSuccess == true)
                            {
                                float fFluidTemp = fFieldValue;
                                // todo: do something with the value...
                            }

                            // query solid temperature
                            bSuccess = oSolidTempField.bGetValue(vecPosition, out fFieldValue);
                            if (bSuccess == true)
                            {
                                float fSolidTemp = fFieldValue;
                                // todo: do something with the value...
                            }
                        }
                    }
                }

                // previews
                Sh.PreviewVoxels(voxFluidDomain, Cp.clrBlue);
                Sh.PreviewVoxels(voxSolidDomain, Cp.clrRock);

                Library.Log("Finished Task.");
                return;
            }

            public static void SimulateHeatTransfer(
                ScalarField oFluidTempField,
                ScalarField oSolidTempField,
                VectorField oVelocityField,
                Voxels voxFluidDomain,
                Voxels voxSolidDomain,
                float fDensity,
                float fSpecificHeat,
                float fThermalConductivity,
                float fTimeStep,
                int nIterations)
            {
                // Calculate thermal diffusivity for the solid
                float fThermalDiffusivity = fThermalConductivity / (fDensity * fSpecificHeat);

                // Get bounding boxes
                BBox3 oFluidBBox = Sh.oGetBoundingBox(voxFluidDomain);
                BBox3 oSolidBBox = Sh.oGetBoundingBox(voxSolidDomain);
                float fStep = 2f; // Spatial resolution for traversal

                // Perform iterations for heat transfer simulation
                for (int iter = 0; iter < nIterations; iter++)
                {
                    // Loop through fluid domain
                    for (float fZ = oFluidBBox.vecMin.Z; fZ <= oFluidBBox.vecMax.Z; fZ += fStep)
                    {
                        for (float fX = oFluidBBox.vecMin.X; fX <= oFluidBBox.vecMax.X; fX += fStep)
                        {
                            for (float fY = oFluidBBox.vecMin.Y; fY <= oFluidBBox.vecMax.Y; fY += fStep)
                            {
                                Vector3 vecPosition = new Vector3(fX, fY, fZ);

                                // Get current fluid temperature
                                if (oFluidTempField.bGetValue(vecPosition, out float fTempFluid))
                                {
                                    // Compute temperature gradients
                                    Vector3 vecGradTemp = FieldOperations.ComputeGradient(oFluidTempField, vecPosition);

                                    // Advection: velocity dot gradient
                                    Vector3 vecVelocity = FieldOperations.GetVector(oVelocityField, vecPosition);
                                    float fAdvection = Vector3.Dot(vecVelocity, vecGradTemp);

                                    // Diffusion: Laplacian of temperature
                                    float fDiffusion = FieldOperations.ComputeLaplacian(oFluidTempField, vecPosition) * fThermalConductivity;

                                    // Update temperature using the energy equation
                                    float fNewTempFluid = fTempFluid + fTimeStep * (-fAdvection + fDiffusion);
                                    oFluidTempField.SetValue(vecPosition, fNewTempFluid);
                                }
                            }
                        }
                    }

                    // Loop through solid domain
                    for (float fZ = oSolidBBox.vecMin.Z; fZ <= oSolidBBox.vecMax.Z; fZ += fStep)
                    {
                        for (float fX = oSolidBBox.vecMin.X; fX <= oSolidBBox.vecMax.X; fX += fStep)
                        {
                            for (float fY = oSolidBBox.vecMin.Y; fY <= oSolidBBox.vecMax.Y; fY += fStep)
                            {
                                Vector3 vecPosition = new Vector3(fX, fY, fZ);

                                // Get current solid temperature
                                if (oSolidTempField.bGetValue(vecPosition, out float fTempSolid))
                                {
                                    // Diffusion in solid
                                    float fDiffusion = FieldOperations.ComputeLaplacian(oSolidTempField, vecPosition) * fThermalDiffusivity;

                                    // Update solid temperature
                                    float fNewTempSolid = fTempSolid + fTimeStep * fDiffusion;
                                    oSolidTempField.SetValue(vecPosition, fNewTempSolid);
                                }
                            }
                        }
                    }
                }

                Library.Log("Heat transfer simulation completed.");
            }        
        }
    }
}
