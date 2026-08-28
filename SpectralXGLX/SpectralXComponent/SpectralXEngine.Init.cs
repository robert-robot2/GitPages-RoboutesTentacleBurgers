

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {


        public void Init()
        {
            // GL Init

            _uploadedTextures.Clear();
            _uploadedMeshes.Clear();
            _gamepad.InitAsync();


            // The Shadow Knows...
            // Initialize scene shadow settings — SpectralXS modes read from this
            Shadow = new SpectralXShadow();

            Shadow.SoftnessBias = 0.008f;   // tighter than before
            Shadow.KernelSize = 3.0f;       // controls overall disk radius
            Shadow.DepthBias = 0.003f;      // reduce acne without over-softening
            Shadow.ContactSharpness = 0.0005f; // decrease for sharper contact shadows
            Shadow.TintR = 0.2f;               // add a warm tint to shadows
            Shadow.TintStrength = 0.3f;        // how strong the tint is
            Shadow.PenumbraTintStrength = 0.4f;  // how much light color bleeds into penumbra edges


            // ── Scene 1 Lighting ────────────────────────────────────────────────────────

            var scene1PointL1 = new SpectralXLight(
                position: new Vector3(0, -2, 10),
                color: new Vector3(1f, 1f, 1f),
                intensity: 5.0f,
                range: 15f);
            scene1PointL1.CastsShadows = true;
            Scene.AddLight(scene1PointL1);

            var scene1PointL2 = new SpectralXLight(
                position: new Vector3(-5, 5, 12),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene1PointL2.CastsShadows = true;
            Scene.AddLight(scene1PointL2);

            var scene1PointL3 = new SpectralXLight(
                position: new Vector3(5, 5, 12),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene1PointL3.CastsShadows = true;
            Scene.AddLight(scene1PointL3);

            // ── Scene 1 Light Gizmos ─────────────────────────────────────────────────────

            // Main white point light gizmo
            var scene1L1Gizmo = CreateGizmoFrom("S1_LightGizmo_L1", "LightBulb");
            scene1L1Gizmo.Position = scene1PointL1.Position;
            scene1L1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene1L1Gizmo.Color = new Vector4(1f, 0.98f, 0.85f, 0.4f);
            scene1L1Gizmo.IsEmissive = true;
            scene1L1Gizmo.CastsShadow = false;
            scene1L1Gizmo.ReceivesShadow = false;
            scene1L1Gizmo.EmissiveIntensity = 0.8f;
            Scene.AddMesh(scene1L1Gizmo);

            var scene1L1Core = CreateGizmoFrom("S1_LightCore_L1", "SmoothSphere");
            scene1L1Core.Position = scene1PointL1.Position;
            scene1L1Core.Size = new Vector3(0.08f, 0.08f, 0.08f);
            scene1L1Core.Color = new Vector4(1f, 0.95f, 0.6f, 1f);
            scene1L1Core.IsEmissive = true;
            scene1L1Core.CastsShadow = false;
            scene1L1Core.ReceivesShadow = false;
            scene1L1Core.EmissiveIntensity = 3.0f;
            Scene.AddMesh(scene1L1Core);

            var scene1L1AuraInner = CreateGizmoFrom("S1_LightAuraInner_L1", "SmoothSphere");
            scene1L1AuraInner.Position = scene1PointL1.Position;
            scene1L1AuraInner.Size = new Vector3(0.35f, 0.35f, 0.35f);
            scene1L1AuraInner.Color = new Vector4(1f, 0.85f, 0.4f, 0.12f);
            scene1L1AuraInner.IsEmissive = true;
            scene1L1AuraInner.CastsShadow = false;
            scene1L1AuraInner.ReceivesShadow = false;
            scene1L1AuraInner.EmissiveIntensity = 1.2f;
            Scene.AddMesh(scene1L1AuraInner);

            var scene1L1AuraOuter = CreateGizmoFrom("S1_LightAuraOuter_L1", "SmoothSphere");
            scene1L1AuraOuter.Position = scene1PointL1.Position;
            scene1L1AuraOuter.Size = new Vector3(0.6f, 0.6f, 0.6f);
            scene1L1AuraOuter.Color = new Vector4(1f, 0.75f, 0.3f, 0.05f);
            scene1L1AuraOuter.IsEmissive = true;
            scene1L1AuraOuter.CastsShadow = false;
            scene1L1AuraOuter.ReceivesShadow = false;
            scene1L1AuraOuter.EmissiveIntensity = 0.6f;
            Scene.AddMesh(scene1L1AuraOuter);

            // Blue point light gizmo
            var scene1L2Gizmo = CreateGizmoFrom("S1_LightGizmo_L2", "SmoothSphere");
            scene1L2Gizmo.Position = scene1PointL2.Position;
            scene1L2Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene1L2Gizmo.Color = new Vector4(0f, 0.4f, 1f, 1f);
            scene1L2Gizmo.IsEmissive = true;
            scene1L2Gizmo.CastsShadow = false;
            scene1L2Gizmo.EmissiveIntensity = 2.0f;
            Scene.AddMesh(scene1L2Gizmo);

            var scene1L2Aura = CreateGizmoFrom("S1_LightAura_L2", "SmoothSphere");
            scene1L2Aura.Position = scene1PointL2.Position;
            scene1L2Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene1L2Aura.Color = new Vector4(0f, 0.4f, 1f, 0.08f);
            scene1L2Aura.IsEmissive = true;
            scene1L2Aura.CastsShadow = false;
            scene1L2Aura.EmissiveIntensity = 0.8f;
            Scene.AddMesh(scene1L2Aura);

            // Purple point light gizmo
            var scene1L3Gizmo = CreateGizmoFrom("S1_LightGizmo_L3", "SmoothSphere");
            scene1L3Gizmo.Position = scene1PointL3.Position;
            scene1L3Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene1L3Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene1L3Gizmo.IsEmissive = true;
            scene1L3Gizmo.CastsShadow = false;
            scene1L3Gizmo.EmissiveIntensity = 2.0f;
            Scene.AddMesh(scene1L3Gizmo);

            var scene1L3Aura = CreateGizmoFrom("S1_LightAura_L3", "SmoothSphere");
            scene1L3Aura.Position = scene1PointL3.Position;
            scene1L3Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene1L3Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene1L3Aura.IsEmissive = true;
            scene1L3Aura.CastsShadow = false;
            scene1L3Aura.EmissiveIntensity = 0.8f;
            Scene.AddMesh(scene1L3Aura);


            // --- Scene Alleway DozerBox---

            var dozerBox = MeshLibrary.GetMesh("FBXDozerBox");
            if (dozerBox != null)
            {
                dozerBox.Position = new Vector3(0, 10, 0);
                dozerBox.Size = new Vector3(5f, 1f, 2f);
                dozerBox.Color = new Vector4(0.36f, 0.25f, 0.20f, 1f);
                Scene.AddMesh(dozerBox);
            }



            var triangle88 = MeshLibrary.GetMesh("FemurBoneSTL");
            if (triangle88 != null)
            {
                triangle88.Position = new Vector3(-2, 7, 2);
                triangle88.Size = new Vector3(1f, 1f, 1f);
                // triangle88.Color = new Vector4(1f, 1f, 0f, 1f);
                // triangle88.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
                Scene.AddMesh(triangle88);
            }

            // 2D Triangles

            var triangle = MeshLibrary.GetMesh("PrimTriangle");
            if (triangle != null)
            {
                triangle.Position = new Vector3(-8, 0, 2);
                triangle.Size = new Vector3(1f, 1f, 1f);
                triangle.Color = new Vector4(1f, 1f, 0f, 1f);
                triangle.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
                Scene.AddMesh(triangle);
            }

            var TriT = MeshLibrary.GetMesh("TriT");
            if (TriT != null)
            {
                Scene.AddMesh(TriT);
                TriT.Position = new Vector3(-8, 2, 2);
                TriT.Size = new Vector3(1f, 1f, 1f);
                TriT.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90



                //  TriT.Rotation += new Vector3(0f, MathF.PI / 2f, 0f);    // Y +90

                // --- TriT Rotation Tests --- pick one at a time and comment the rest ---

                // X axis rotations
                //  TriT.Rotation += new Vector3(-MathF.PI / 2f, 0f, 0f);     // X -90
                //  TriT.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
                //TriT.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180

                // Y axis rotations
                //TriT.Rotation += new Vector3(0f, -MathF.PI / 2f, 0f);   // Y -90

                //TriT.Rotation += new Vector3(0f, MathF.PI, 0f);          // Y 180

                // Z axis rotations
                //TriT.Rotation += new Vector3(0f, 0f, -MathF.PI / 2f);   // Z -90
                //TriT.Rotation += new Vector3(0f, 0f, MathF.PI / 2f);    // Z +90
                //TriT.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180

                // Combined (for later)
                //TriT.Rotation += new Vector3(-MathF.PI / 2f, 0f, MathF.PI); // X -90 + Z 180  TriT.Rotation += new Vector3(-MathF.PI / 2f, 0f, 0f);



            }



            // 3D TetraHedron


            var fbxisoPyr = MeshLibrary.GetMesh("FBXIsoPyramid");
            if (fbxisoPyr != null)
            {
                Scene.AddMesh(fbxisoPyr);
                fbxisoPyr.Position = new Vector3(-8, 4, 2);
                fbxisoPyr.Color = new Vector4(0.85f, 0.44f, 0.84f, 1f);
            }


            var fbxisoPyrT = MeshLibrary.GetMesh("FBXIsoPyramidT");
            if (fbxisoPyrT != null)
            {
                Scene.AddMesh(fbxisoPyrT);
                fbxisoPyrT.Position = new Vector3(-8, 6, 2);
                fbxisoPyrT.Color = new Vector4(0.85f, 0.44f, 0.84f, 1f);
            }




            // entrance to Scene2


            var square2 = MeshLibrary.GetMesh("PrimSquare");
            if (square2 != null)
            {
                square2.Name = "PortalSquare"; // unique name!
                Scene.AddMesh(square2);
                square2.Position = new Vector3(0, 11, 11);
                square2.Size = new Vector3(1f, 1f, 1f);
                //   square2.Color = new Vector4(0f, 0f, 1f, 0.5f);
                square2.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90

            }

            if (square2 is SpectralXMesh portalMesh)
            {
                portalMesh.IsAnimated = true;
                portalMesh.FrameCount = 10;
                portalMesh.FrameRate = 10f;
                portalMesh.SheetWidth = 840f;
                portalMesh.SheetHeight = 84f;
                portalMesh.FramePixelWidth = 84f;
                portalMesh.FramePixelHeight = 84f;
                portalMesh.TextureDataUrl = "iAssets/PortalSheet001.png";
                portalMesh.TextureIsRawRGBA = false;
            }





            // 2D Squares
            var square88 = MeshLibrary.GetMesh("MechArm");
            if (square88 != null)
            {
                Scene.AddMesh(square88);
                square88.Position = new Vector3(0, 12, 12);
                square88.Size = new Vector3(0.3f, 0.3f, 0.3f);
                //      square88.Color = new Vector4(0f, 1f, 0f, 0.5f);
                square88.Rotation += new Vector3(-MathF.PI / 2f, 0f, 0f);    // X +90
                                                                             // shadow casting is on my default.
                                                                             //  square.CastsShadow = false;
            }


            var square = MeshLibrary.GetMesh("PrimSquare");
            if (square != null)
            {
                square.Name = "GreenSquare";
                Scene.AddMesh(square);
                square.Position = new Vector3(-2, 0, 2);
                square.Size = new Vector3(1f, 1f, 1f);
                square.Color = new Vector4(0f, 1f, 0f, 0.5f);
             //   square.Rotation += new Vector3(-MathF.PI / 2f, 0f, 0f);
                square.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
              
            }


            var plane = MeshLibrary.GetMesh("ColaSquare");
            if (plane != null)
            {
                Scene.AddMesh(plane);
                plane.Position = new Vector3(-2, 2, 2);
                plane.Size = new Vector3(1f, 1f, 1f);
               // plane.Color = new Vector4(0f, 0f, 1f, 1f);
                plane.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
            }

            var cheeseSign = MeshLibrary.GetMesh("CheeseSign");
            if (cheeseSign != null)
            {
                Scene.AddMesh(cheeseSign);
                cheeseSign.Position = new Vector3(-2, 4, 2);
                cheeseSign.Size = new Vector3(1f, 1f, 1f);
                cheeseSign.Color = new Vector4(1f, 1f, 1f, 1f);
                cheeseSign.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
            }





            // --- 3D Cube ---
            var cube = MeshLibrary.GetMesh("PrimCube");
            if (cube != null)
            {
                cube.Position = new Vector3(1, 1, 2);
                cube.Size = new Vector3(1f, 1f, 1f);
                cube.Color = new Vector4(1f, 0f, 0f, 1f);
                Scene.AddMesh(cube);
            }

            var cube2 = MeshLibrary.GetMesh("FBXCubeRed");
            if (cube2 != null)
            {
                cube2.Position = new Vector3(1, 7, 2);
                cube2.Size = new Vector3(1f, 1f, 1f);
                // cube2.Color = new Vector4(1f, 0f, 0f, 1f);
                Scene.AddMesh(cube2);
            }


            var brickbox = MeshLibrary.GetMesh("BrickBox");
            if (brickbox != null)
            {
                Scene.AddMesh(brickbox);
                brickbox.Position = new Vector3(1, 4, 2);
                brickbox.Size = new Vector3(1f, 1f, 1f);

                brickbox.Size = new Vector3(1f, 1f, 1f);
            }






            // 3D Pyramid

            var pyramid = MeshLibrary.GetMesh("PrimPyramid");
            if (pyramid != null)
            {
                pyramid.Position = new Vector3(-5, 1, 2);
                pyramid.Size = new Vector3(1f, 1f, 1f);
                pyramid.Color = new Vector4(1f, 0f, 1f, 1f);
                Scene.AddMesh(pyramid);
            }





            var fbxPyr = MeshLibrary.GetMesh("FBXPyramid");
            if (fbxPyr != null)
            {
                Scene.AddMesh(fbxPyr);
                fbxPyr.Position = new Vector3(-5, 4, 2);
                fbxPyr.Color = new Vector4(0f, 0f, 1f, 1f);
            }

            var fbxPyrT = MeshLibrary.GetMesh("FBXPyramidT");
            if (fbxPyrT != null)
            {
                Scene.AddMesh(fbxPyrT);
                fbxPyrT.Position = new Vector3(-5, 7, 2);
             //   fbxPyrT.Color = new Vector4(0f, 0f, 1f, 1f);
            }

            // 3D Sphere

            var sphere = MeshLibrary.GetMesh("FBXSphere");
            if (sphere != null)
            {
                sphere.Position = new Vector3(4, 1, 2);
                sphere.Size = new Vector3(1f, 1f, 1f);
                sphere.Color = new Vector4(1f, 0.5f, 0f, 1f);
                Scene.AddMesh(sphere);
            }

            // need smooth shading
            var smoothsphere = MeshLibrary.GetMesh("SmoothSphereStatic");
            if (smoothsphere != null)
            {
                smoothsphere.Position = new Vector3(4, 4, 2);
                smoothsphere.Size = new Vector3(1f, 1f, 1f);
                smoothsphere.Color = new Vector4(1f, 0.5f, 0f, 0.75f);
                (smoothsphere as SpectralXMesh).CastsShadow = true;    
                (smoothsphere as SpectralXMesh).ReceivesShadow = true;
                Scene.AddMesh(smoothsphere);
            }



            // textures not working
            var smoothsphereT = MeshLibrary.GetMesh("SmoothSphereT");
            if (smoothsphereT != null)
            {
                smoothsphereT.Position = new Vector3(4, 7, 2);
                smoothsphereT.Size = new Vector3(1f, 1f, 1f);
                smoothsphereT.Color = new Vector4(1f, 0.5f, 0f, 1f);
                Scene.AddMesh(smoothsphereT);
            }

            // 3d hex Cylinder

            var hexCyl = MeshLibrary.GetMesh("HexCyl");
            if (hexCyl != null)
            {
                Scene.AddMesh(hexCyl);
                hexCyl.Position = new Vector3(7, 1, 2);
                hexCyl.Size = new Vector3(1f, 1f, 1f);
                hexCyl.Color = new Vector4(0f, 1f, 1f, 1f);
            }

            // textures not working

            var hexCylT = MeshLibrary.GetMesh("HexCylT");
            if (hexCylT != null)
            {
                Scene.AddMesh(hexCylT);
                hexCylT.Position = new Vector3(7, 4, 2);
                hexCylT.Size = new Vector3(1f, 1f, 1f);
                //  hexCylT.Color = new Vector4(0f, 1f, 1f, 1f);
            }

            // Light Test

            var LightBulb1 = MeshLibrary.GetMesh("LightBulbStatic");
            if (LightBulb1 != null)
            {
                LightBulb1.Position = new Vector3(7, 7, 2);
                LightBulb1.Size = new Vector3(1f, 1f, 1f);
                LightBulb1.Color = new Vector4(1f, 1f, 1f, 0.25f);
                (LightBulb1 as SpectralXMesh).CastsShadow = true;
                (LightBulb1 as SpectralXMesh).ReceivesShadow = true;
                Scene.AddMesh(LightBulb1);
            }

            // ── Font Registration ────────────────────────────────────────────────────
            MeshLibrary.RegisterFont("Diablo",
                "/iAssets/Fonts/DiabloAtlas.json",
                "/iAssets/Fonts/DiabloAtlas.png");

            // ── Welcome Text ─────────────────────────────────────────────────────────
            var welcomeText = AddText("WELCOME",
                position: new Vector3(-3f, 0f, 8f),
                fontSize: 2f,
                fontKey: "Diablo",
                color: new Vector4(1f, 0.8f, 0.2f, 1f),
                align: TextAlignment.Center);
            welcomeText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            welcomeText.GlowRadius = 0.2f;
            welcomeText.GlowStrength = 1.0f;
            welcomeText.EmissiveIntensity = 3.0f;


            Camera.Position = new CustomVec3(0, -10, 4);
            Input.Register();
        }









    }
}
