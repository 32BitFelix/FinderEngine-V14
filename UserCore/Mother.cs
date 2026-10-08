using OpenTK.Mathematics;
using Core.LECSSimple;
using Core;
using Core.Shimshek;
using IO.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using IO;
using System.Runtime.InteropServices;
using Core.SAS2D;
using Core.MemoryManagement;
using System.Runtime.CompilerServices;


namespace UserCore.Mother;




[Level(true)]
public unsafe static class Main
{

    public static int LevelID;


    public static int pCamera, Redhead, Hourglass;


    public static int groundBox;


    public static int box;


    public static int* ents;


    public static int* minkSumPoints;

    public static int* lineSegmentPoints;


    public static void Start()
    {
        const int compAmount = 4;

        int* comps = stackalloc int[compAmount];

        comps[0] = Transform.ComponentID;

        comps[1] = Sprite.ComponentID;

        comps[2] = Collider2D.ComponentID;

        comps[3] = RigidBody2D.ComponentID;


        int playerColMaterial = 0;

        int playerRbMaterial = 0;

        {
            ColliderMaterial2D colMat = new();

            playerColMaterial = ColliderMaterial2D.AddMaterial(colMat);


            RigidBodyMaterial2D rbMat;

            rbMat.Restitution = 0;

            rbMat.Roughness = 0;

            playerRbMaterial = RigidBodyMaterial2D.AddMaterial(rbMat);
        }


        Vector2* vertices = CompactArray.Create<Vector2>(4);

        vertices[0] = ( -1, 1);

        vertices[1] = ( 1,  1);

        vertices[2] = ( 1, -1);

        vertices[3] = (-1, -1);


        fixed(int* ptr = &Redhead)
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(Redhead, comps, compAmount);

        {
            Transform* tran = (Transform*)Finder.GetComponent(Redhead, Transform.ComponentID);

            tran->Scale = (0.5f, 0.5f, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, -5, -40, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(Redhead, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create("./Resources/Textures/Tri.png", false);


            Collider2D* col = (Collider2D*)Finder.GetComponent(Redhead, Collider2D.ComponentID);

            col->SetPolygon(vertices);

            col->MaterialID = playerColMaterial;


            RigidBody2D* rb = (RigidBody2D*)Finder.GetComponent(Redhead, RigidBody2D.ComponentID);

            rb->MaterialID = playerRbMaterial;

            rb->Mass = 0;
        }


        int objColMaterial = 0;

        int objRbMaterial = 0;

        {
            ColliderMaterial2D colMat = new();

            objColMaterial = ColliderMaterial2D.AddMaterial(colMat);


            RigidBodyMaterial2D rbMat;

            rbMat.Restitution = 0;

            rbMat.Roughness = 0;

            objRbMaterial = RigidBodyMaterial2D.AddMaterial(rbMat);
        }


        fixed(int* ptr = &Hourglass)
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(Hourglass, comps, compAmount);


        {
            int rotatComp = Rotator.ComponentID;

            Finder.AddComponents(Hourglass, &rotatComp, 1);
        }


        {
            Transform* tran = (Transform*)Finder.GetComponent(Hourglass, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (2, 0, 0, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(Hourglass, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create("./Resources/Textures/Hourglass.png", false);


            Rotator* rot = (Rotator*)Finder.GetComponent(Hourglass, Rotator.ComponentID);

            rot->Speed = 10;


            Collider2D* col = (Collider2D*)Finder.GetComponent(Hourglass, Collider2D.ComponentID);

            col->SetPolygon(vertices);

            col->MaterialID = objColMaterial;


            RigidBody2D* rb = (RigidBody2D*)Finder.GetComponent(Hourglass, RigidBody2D.ComponentID);

            rb->MaterialID = objRbMaterial;

            rb->Mass = 0;
        }


        int groundColMaterial = 0;

        int groundRbMaterial = 0;

        {
            ColliderMaterial2D colMat = new();

            groundColMaterial = ColliderMaterial2D.AddMaterial(colMat);


            RigidBodyMaterial2D rbMat;

            rbMat.Restitution = 0;

            rbMat.Roughness = 0;

            groundRbMaterial = RigidBodyMaterial2D.AddMaterial(rbMat);
        }


        int* groundComps = stackalloc int[3];

        groundComps[0] = Transform.ComponentID;

        groundComps[1] = Sprite.ComponentID;

        groundComps[2] = Collider2D.ComponentID;


        fixed(int* ptr = &groundBox)
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(groundBox, groundComps, 3);

        {
            Transform* tran = (Transform*)Finder.GetComponent(groundBox, Transform.ComponentID);

            tran->Scale = (10, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, -2, 0, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(groundBox, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            int tex = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create((byte*)&tex, 1, 1, false);


            Collider2D* col = (Collider2D*)Finder.GetComponent(groundBox, Collider2D.ComponentID);

            col->SetPolygon(vertices);

            col->MaterialID = groundColMaterial;


            /*RigidBody2D* rb = (RigidBody2D*)Finder.GetComponent(groundBox, RigidBody2D.ComponentID);

            rb->MaterialID = groundRbMaterial;

            rb->Mass = 0;*/
        }


        int* camComps = stackalloc int[2];

        camComps[0] = Transform.ComponentID;

        camComps[1] = Camera.ComponentID;


        fixed(int* ptr = &pCamera)    
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(pCamera, camComps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(pCamera, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, 0, 10, 0);


            Camera* cam = (Camera*)Finder.GetComponent(pCamera, Camera.ComponentID);

            cam->FarClip = 100;

            cam->NearClip = 0.1f;

            cam->FieldOfView = 90;

            cam->ProjectionSize = 30.0f;
        }


        CompactArray.Delete(vertices);


        fixed(int* ptr = &box)
            Finder.CreateEntities(ptr, 1);

        int* boxComps = stackalloc int[2];

        boxComps[0] = Transform.ComponentID;

        boxComps[1] = Sprite.ComponentID;

        Finder.AddComponents(box, boxComps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(box, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 0);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, -5, -50, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(box, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create("./Resources/Textures/Tri.png", false);
        }


        int* pointComps = stackalloc int[2];

        pointComps[0] = Transform.ComponentID;

        pointComps[1] = Sprite.ComponentID;


        const int minksumPointAmount = 6;

        minkSumPoints = (int*)NativeMemory.Alloc(sizeof(int) * 6);

        Finder.CreateEntities(minkSumPoints, minksumPointAmount);


        for(int i = 0; i < minksumPointAmount; i++)
        {
            Finder.AddComponents(minkSumPoints[i], pointComps, 2);


            Transform* tran = (Transform*)Finder.GetComponent(minkSumPoints[i], Transform.ComponentID);

            tran->Scale = (0.1f, 0.1f, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, -7, 9.8f, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(minkSumPoints[i], Sprite.ComponentID);

            spr->RGBA = ((255 - (255 / minksumPointAmount * i)) << 8) | (255 << 24);

            int tex = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create((byte*)&tex, 1, 1, false);
        }


        lineSegmentPoints = (int*)NativeMemory.Alloc(sizeof(int) * 2);

        Finder.CreateEntities(lineSegmentPoints, 2);


        for(int i = 0; i < 2; i++)
        {
            Finder.AddComponents(lineSegmentPoints[i], pointComps, 2);


            Transform* tran = (Transform*)Finder.GetComponent(lineSegmentPoints[i], Transform.ComponentID);

            tran->Scale = (0.1f, 0.1f, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, -7, 9.8f, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(lineSegmentPoints[i], Sprite.ComponentID);

            spr->RGBA = ((255 / (i + 1)) << 16) | (255 << 24);

            int tex = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create((byte*)&tex, 1, 1, false);
        }



        Finder.FixedUpdatesPerSecond = 15;



        /*Triangle triA;

        triA[0] = (0, 1, 0, 0);

        triA[1] = (1, 0, 0, 0);

        triA[2] = (-1, 0, 0, 0);


        Triangle triB;

        triB[0] = (-1, 1, 0, 0);

        triB[1] = (1, 1, 0, 0);

        triB[2] = (0, 0, 0, 0);


        bool colling = TrisCollideContinous2D(&triA, &triB, (0, 0), (0, 0), out Vector2 n, out float d);

        Console.WriteLine();*/


        /*WindowManager.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;

        WindowManager.CursorState = CursorModeValue.CursorDisabled;*/
    }


    public static void Update()
    {  
        if(KBM.IsHeld((int)Keys.Escape))
            WindowManager.CloseWindow();


        /*// Look stuff

        camTran->Rotation.Y += KBM.CursorVelocity.X;

        camTran->Rotation.X += KBM.CursorVelocity.Y;


        camTran->Rotation.X = Math.Clamp(camTran->Rotation.X, -90, 90);*/
    }


    // The crossproduct of two vectors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CrossProduct(Vector2 a, Vector2 b)
        => a.X * b.Y - a.Y * b.X;


    // Conducts a continous collision test
    // between two triangles, based on their
    // linear velocities

    public static bool TrisCollideContinous2D(Triangle* a, Triangle* b,
        Vector2 aVel, Vector2 bVel, out Vector2 normal, out float depth)
    {
        normal = (0, 0);

        depth = float.MaxValue;


        // The points of the line segment
        // to check the intersection with

        Vector2 pointA, pointB;

        
        // Evaluate the points of the line segment

        {
            // Calculate the center of both triangles

            Vector2 aCenter = (a[0][0].Xy + a[0][1].Xy + a[0][2].Xy) * (1f / 3); // Inverse is calculated at compile time

            Vector2 bCenter = (b[0][0].Xy + b[0][1].Xy + b[0][2].Xy) * (1f / 3);


            /*// If any of the triangles appear to be a point,
            // then prematurely end the method

            if(aCenter == a[0][0].Xy)
                return false;

            if(bCenter == b[0][0].Xy)
                return false;*/


            // The euclidean difference between
            // both triangles

            Vector2 normDiff = bCenter - aCenter;


            // The center of the minkowski sum

            Vector2 sumCenter = bCenter + aCenter;


            // Define the points of the line segment

            pointB = sumCenter - normDiff;

            pointA = pointB + (bVel - aVel);
        }


        {
            Transform* t = (Transform*)Finder.GetComponent(lineSegmentPoints[0], Transform.ComponentID);

            t->Translation.Xy = pointA;
        }

        {
            Transform* t = (Transform*)Finder.GetComponent(lineSegmentPoints[1], Transform.ComponentID);

            t->Translation.Xy = pointB;
        }


        byte minkI = 0;

        // The amount of vertices of a minowski sum of
        // convex polygons always is, at most,
        // the sum of the amount of the polygon's vertices

        Vector2* minkSumVerts = stackalloc Vector2[6];  


        // Find the lowest and leftmost point of triangle A

        byte iStart = 0;

        for(int i = 1; i < 3; i++)
        {
            bool check = (a[0][i].Y < a[0][iStart].Y) | (a[0][i].Y == a[0][iStart].Y & a[0][i].X < a[0][iStart].X);

            if(check)
                iStart = (byte)i;
        }


        // Find the lowest and leftmost point of triangle B

        byte jStart = 0;

        for(int j = 1; j < 3; j++)
        {
            bool check = (b[0][j].Y < b[0][jStart].Y) | (b[0][j].Y == b[0][jStart].Y & b[0][j].X < b[0][jStart].X);

            if(check)
                jStart = (byte)j;
        }


        byte condition = (byte)(iStart + 3);

        while(iStart < condition)
        {
            Vector2 edgeI = a[0][(iStart + 1) % 3].Xy - a[0][iStart % 3].Xy;

            Vector2 edgeJ = b[0][(jStart + 1) % 3].Xy - b[0][jStart % 3].Xy;


            float cross = CrossProduct(edgeI, edgeJ);


            minkSumVerts[minkI++] = a[0][iStart % 3].Xy + b[0][jStart % 3].Xy;


            if(cross <= 0)
                iStart++;
            
            if(cross >= 0)
                jStart++;
        }



        for(int i = 0; i < 6; i++)
        {
            Transform* t = (Transform*)Finder.GetComponent(minkSumPoints[i], Transform.ComponentID);

            t->Translation.Xy = minkSumVerts[i];
        }


        if(pointB - pointA == (0, 0))
            return false;


        /*for(int i = 0; i < 6; i++)
        {
            Vector2 axis = minkSumVerts[(i + 1) % 6] - minkSumVerts[i];

            //Vector2 axis = pointB - pointA;

            axis = (axis.Y, -axis.X);

            // Axis is normalized with a fast approximation
            // of it's inverse length. Avoids normalization
            // on the projections

            {
                float invLen = axis.X * axis.X + axis.Y * axis.Y;

                invLen = float.ReciprocalSqrtEstimate(invLen);

                axis *= invLen;
            }


            // Find the projection closest
            // to the axis

            float max;

            {
                Vector2 segToFirst = pointA - minkSumVerts[i];

                max = Vector2.Dot(segToFirst, axis);
            }


            /*{
                Vector2 segToSecond = pointB - minkSumVerts[i];

                float projB = Vector2.Dot(segToSecond, axis);

                if(projB > max)
                    max = projB;
            }*/


            // If there is no projection on the
            // negative side of the axis, then
            // there is no collision to speak of

            /*if(max < 0)
                return false;


            Console.WriteLine(max);


            if(max < depth)
            {
                depth = max;

                normal = axis;
            }
        }*/


        /*// Ensures, that the seperating normal
        // doesn't make the triangles seperate into eachother
    
        Vector2 aAVRG = a[0][0].Xy + a[0][1].Xy + a[0][2].Xy;

        Vector2 bAVRG = b[0][0].Xy + b[0][1].Xy + b[0][2].Xy;

        Vector2 direction = (bAVRG - aAVRG) * (1f / 3);

        if(Vector2.Dot(direction, normal) > 0.0f)
            normal = -normal;*/


        return false;    
    }

    // A structure, that simply
    // represents a polygon with
    // three vertices: a triangle

    [InlineArray(3)]
    public struct Triangle
    {
        // The individual vertices
        // of the triangle

        public Vector4 Vertex;
    }


    // Seperate triangles, if they intersect in a continous way

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SeperateTrisContinous(Triangle* a, Transform* aTran, Vector2 aVel,
                                             Triangle* b, Transform* bTran, Vector2 bVel)
    {
        bool intersects = TriTriContinous2D(a, aVel, b, bVel, out float depth, out Vector2 normal);

        if(intersects)
        {
            aTran->Translation.Xy += normal * depth * 0.5f;

            bTran->Translation.Xy -= normal * depth * 0.5f;
        }
    }


    // Continuously checks for collision between two triangles

    public static bool TriTriContinous2D(Triangle* a, Vector2 aVel,
                                         Triangle* b, Vector2 bVel,
                                         out float depth, out Vector2 normal)
    {
        normal = (0, 0);

        depth = float.MaxValue;


        // The points of the line segment
        // to check the intersection with

        Vector2 pointA, pointB;

        
        // Evaluate the points of the line segment

        {
            // Calculate the center of both triangles

            Vector2 aCenter = (a[0][0].Xy + a[0][1].Xy + a[0][2].Xy) * (1f / 3); // Inverse is calculated at compile time

            Vector2 bCenter = (b[0][0].Xy + b[0][1].Xy + b[0][2].Xy) * (1f / 3);


            /*// If any of the triangles appear to be a point,
            // then prematurely end the method

            if(aCenter == a[0][0].Xy)
                return false;

            if(bCenter == b[0][0].Xy)
                return false;*/


            // The euclidean difference between
            // both triangles

            Vector2 normDiff = bCenter - aCenter;


            // The center of the minkowski sum

            Vector2 sumCenter = bCenter + aCenter;


            // Define the points of the line segment

            pointB = sumCenter - normDiff;

            pointA = pointB + (bVel - aVel);
        }


        {
            Transform* t = (Transform*)Finder.GetComponent(lineSegmentPoints[0], Transform.ComponentID);

            t->Translation.Xy = pointA;
        }

        {
            Transform* t = (Transform*)Finder.GetComponent(lineSegmentPoints[1], Transform.ComponentID);

            t->Translation.Xy = pointB;
        }        


        // Construct the minkowski sum
        // of both triangles

        byte minkI = 0;

        Vector2* minkSumVerts = stackalloc Vector2[6];


        // Find the starting point for the
        // construction of the minkowski sum

        byte startA = 0;

        {
            float lastCP = float.MaxValue;

            for(byte i = 0; i < 3; i++)
            {
                float cross = CrossProduct(a[0][(i + 1) % 3].Xy - a[0][i].Xy, b[0][1].Xy - b[0][0].Xy);

                if(cross < lastCP)
                {
                    lastCP = cross;

                    startA = i;
                }

                if(cross > lastCP)
                    break;
            }

        }

        for(byte i = 0; i < 12;)
        {
            (byte indexA, byte indexB) = byte.DivRem(i, 3);

            indexA = (byte)((indexA + startA) % 3);

            Vector2 edgeA = a[0][(indexA + 1) % 3].Xy - a[0][indexA % 3].Xy;

            Vector2 edgeB = b[0][(indexB + 1) % 3].Xy - b[0][indexB % 3].Xy;

            
            float cross = CrossProduct(edgeA, edgeB);


            Console.WriteLine(indexA + " " + indexB + " " + minkI);

            minkSumVerts[minkI++] = a[0][indexA % 3].Xy + b[0][indexB % 3].Xy;


            if(minkI > 5)
                break;


            if(((*(uint*)&cross & 0x80_00_00_00) == 0x80_00_00_00) || ((*(uint*)&cross & 0x7F_FF_FF_FF) == 0x00_00_00_00))
                i += 3;

            if((*(uint*)&cross & 0x80_00_00_00) == 0x00_00_00_00)
                i++;
        }


        /*byte startA = 0;
        
        byte startB = 0;

        for(int i = 1; i < 3; i++)
        {
            bool checkA = (a[0][i].Y < a[0][startA].Y) | (a[0][i].Y == a[0][startA].Y & a[0][i].X < a[0][startA].X);

            bool checkB = (b[0][i].Y < b[0][startB].Y) | (b[0][i].Y == b[0][startB].Y & b[0][i].X < b[0][startB].X);

            if(checkA)
                startA = (byte)i;

            if(checkB)
                startB = (byte)i;
        }

        for(byte condition = (byte)(startA + 3); startA < condition;)
        {
            Vector2 edgeA = a[0][(startA + 1) % 3].Xy - a[0][startA % 3].Xy;

            Vector2 edgeB = b[0][(startB + 1) % 3].Xy - b[0][startB % 3].Xy;


            float cross = CrossProduct(edgeA, edgeB);


            minkSumVerts[minkI++] = a[0][startA % 3].Xy + b[0][startB % 3].Xy;


            if(cross <= 0)
                startA++;
            
            if(cross >= 0)
                startB++;
        }*/


        for(int i = 0; i < 6; i++)
        {
            Transform* t = (Transform*)Finder.GetComponent(minkSumPoints[i], Transform.ComponentID);

            t->Translation.Xy = minkSumVerts[i];
        }


        if(pointB - pointA == (0, 0))
            return false;


        return false;
    }


    // Seperates triangles, if they intersect

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SeperateTris(Triangle* a, Transform* aTran, Triangle* b, Transform* bTran)
    {
        bool intersects = TriangleIntersectsTriangle2D(a, b, out float depth, out Vector2 normal);

        if(intersects)
        {
            aTran->Translation.Xy += normal * depth * 0.5f;

            bTran->Translation.Xy -= normal * depth * 0.5f;
        }
    }


    // Checks for collision between two triangles

    public static bool TriangleIntersectsTriangle2D(Triangle* a, Triangle* b,
        out float depth, out Vector2 normal)
    {
        depth = float.MaxValue;

        normal = (0, 0);


        /*// Calculate a multiplier, that'll ensure,
        // that the normals evaluated from the triangles
        // are always pointing towards the
        // center of the triangles

        sbyte xMult = 0;

        sbyte yMult = 0;

        {
            float allSum = (b[0][0].X - b[0][1].X) * (b[0][0].Y + b[0][1].Y);

            allSum += (b[0][1].X - b[0][2].X) * (b[0][1].Y + b[0][2].Y);


            xMult = (sbyte)(allSum >= 0 ? 1 : -1);

            yMult = (sbyte)(xMult ^ 0xFE);
        }*/


        // Project triangle A to triangle B

        for(byte i = 0; i < 3; i++)
        {
            Vector2 axis = b[0][(i + 1) % 3].Xy - b[0][i].Xy;

            //axis = (axis.Y * yMult, axis.X * xMult);

            axis = (axis.Y, -axis.X);

            // Axis is normalized with a fast approximation
            // of it's inverse length. Avoids normalization
            // on the projections

            {
                float invLen = axis.X * axis.X + axis.Y * axis.Y;

                invLen = float.ReciprocalSqrtEstimate(invLen);

                axis *= invLen;
            }


            // Project the vertices of triangle a
            // to the current axis

            float projA;

            {
                Vector2 b_To_aA = a[0][0].Xy - b[0][i].Xy;

                projA = Vector2.Dot(b_To_aA, axis);
            }

            float projB;

            {
                Vector2 b_To_aB = a[0][1].Xy - b[0][i].Xy;

                projB = Vector2.Dot(b_To_aB, axis);
            }

            float projC;

            {
                Vector2 b_To_aC = a[0][2].Xy - b[0][i].Xy;

                projC = Vector2.Dot(b_To_aC, axis);
            }


            // Evaluate the projection closest
            // to the axis

            float max = projA > projB ? projA : projB;

            if(projC > max)
                max = projC;


            // If there is no projection on the
            // negative side of the axis, then
            // there is no collision to speak of

            if(max < 0)
                return false;


            // Save the penetration values
            // of the current axis, if they
            // appear to be more shallow, than
            // the currently saved one

            if(max < depth)
            {
                depth = max;

                normal = axis;
            }
        }

        // Negate normal, because reasons

        normal *= -1;



        /*{
            float allSum = (a[0][0].X - a[0][1].X) * (a[0][0].Y + a[0][1].Y);

            allSum += (a[0][1].X - a[0][2].X) * (a[0][1].Y + a[0][2].Y);
            

            xMult = (sbyte)(allSum >= 0 ? 1 : -1);

            yMult = (sbyte)(xMult ^ 0xFE);
        }*/


        // Project triangle B to triangle A

        for(byte i = 0; i < 3; i++)
        {
            Vector2 axis = a[0][(i + 1) % 3].Xy - a[0][i].Xy;

            //axis = (axis.Y * yMult, axis.X * xMult);

            axis = (axis.Y, -axis.X);

            {
                float invLen = axis.X * axis.X + axis.Y * axis.Y;

                invLen = float.ReciprocalSqrtEstimate(invLen);

                axis *= invLen;
            }


            float projA;

            {
                Vector2 a_To_bA = b[0][0].Xy - a[0][i].Xy;

                projA = Vector2.Dot(a_To_bA, axis);
            }

            float projB;

            {
                Vector2 a_To_bB = b[0][1].Xy - a[0][i].Xy;

                projB = Vector2.Dot(a_To_bB, axis);
            }

            float projC;

            {
                Vector2 a_To_bC = b[0][2].Xy - a[0][i].Xy;

                projC = Vector2.Dot(a_To_bC, axis);
            }


            float max = projA > projB ? projA : projB;

            if(projC > max)
                max = projC;


            if(max < 0)
                return false;


            if(max < depth)
            {
                depth = max;

                normal = axis;
            }
        }


        return true;
    }


    private static Vector2 lastAPos, lastBPos;


    public static void NarrowPhase()
    {
        Transform* rhTran = (Transform*)Finder.GetComponent(Redhead, Transform.ComponentID);

        bool up = KBM.IsHeld((int)Keys.W);

        bool down = KBM.IsHeld((int)Keys.S);

        bool left = KBM.IsHeld((int)Keys.A);

        bool right = KBM.IsHeld((int)Keys.D);


        bool space = KBM.IsHeld((int)Keys.Space);

        bool c = KBM.IsHeld((int)Keys.C);


        const float speed = 5;

        const float speedMult = 2.0f;


        float delta = speed * Finder.FixedDeltaTime;


        // Speed stuff

        delta *= KBM.IsHeld((int)Keys.LeftShift) ? speedMult : 1;


        // Movement stuff

        rhTran->Translation.X += (*(byte*)&right & 1) * delta;

        rhTran->Translation.X -= (*(byte*)&left & 1) * delta;


        rhTran->Translation.Y += (*(byte*)&up & 1) * delta;

        rhTran->Translation.Y -= (*(byte*)&down & 1) * delta;


        Triangle triA = new();

        triA[0] = new Vector4(-1f,  1f, 0f, 1f) * rhTran->GetModelMatrix(Redhead);

        triA[1] = new Vector4( 1f, -1f, 0f, 1f) * rhTran->GetModelMatrix(Redhead);

        triA[2] = new Vector4(-1f, -1f, 0f, 1f) * rhTran->GetModelMatrix(Redhead);

        //rhTran->Rotation.Z = 90f;


        Transform* boxTran = (Transform*)Finder.GetComponent(box, Transform.ComponentID);

        //boxTran->Rotation.Z += Finder.FixedDeltaTime * 5;

        boxTran->Rotation.Z = 0.0f;

        Triangle triB = new();

        triB[0] = new Vector4(-1f,  1f, 0f, 1f) * boxTran->GetModelMatrix(box);

        triB[1] = new Vector4( 1f, -1f, 0f, 1f) * boxTran->GetModelMatrix(box);

        triB[2] = new Vector4(-1f, -1f, 0f, 1f) * boxTran->GetModelMatrix(box);


        //Console.WriteLine(triB[0] + " " + triB[1] + " " + triB[2]);


        //Console.WriteLine(boxTran->Rotation.Z);


        Vector2 velocityA = rhTran->GetGlobalTranslation(Redhead).Xy - lastAPos;

        Vector2 velocityB = boxTran->GetGlobalTranslation(box).Xy - lastBPos;

        lastAPos = rhTran->GetGlobalTranslation(Redhead).Xy;

        lastBPos = boxTran->GetGlobalTranslation(box).Xy;

        SeperateTrisContinous(&triA, rhTran, velocityA, &triB, boxTran, velocityB);

        //_ = TrisCollideContinous2D(&triA, &triB, velocityA, velocityB, out Vector2 n, out float d);



        //SeperateTris(&triA, rhTran, &triB, boxTran);
    }


    public static void BroadPhase()
    {



    }


    public static void PreRender()
    {



    }


    public static void Render()
    {



    }


    public static void PostRender()
    {



    }


    public static void End()
    {

        NativeMemory.Free(ents);

        Console.WriteLine("FREED");
    }
}


[System(false, false)]
public unsafe static class TransformProcessor
{
    public static int[] Init()
        => [Transform.ComponentID];

    public static void Update(ArchetypeIterator* iter)
    {



    }
}


[System(false, false)]
public unsafe static class A_Processor
{
    public static int[] Init()
        => [A.ComponentID];


    public static void Update(ArchetypeIterator* iter)
    {

        //Console.WriteLine("LESSER");

    }

} 


[Component]
public struct Rotator
{
    public static int ComponentID;

    public float Speed;
}


[System(false, false)]
public unsafe static class RotatorSystem
{
    public static int[] Init()
        => [Transform.ComponentID, Rotator.ComponentID];


    public static float counter;

    public static void Update(ArchetypeIterator* iter)
    {
        float sinScale = MathF.Sin(counter * 5) * 0.5f + 0.5f;

        counter += Finder.DeltaTime;


        float delta = Finder.DeltaTime * 20 * sinScale;

        for(int i = 0; i < iter->Length(); i++)
        {
            if(iter->GetEntityID(i) < 2)
                continue;


            Transform* tran = (Transform*)iter->GetComponent(i, Transform.ComponentID);

            Rotator* rot = (Rotator*)iter->GetComponent(i, Rotator.ComponentID);

            tran->Rotation.Z += delta * rot->Speed;
        }


    }

}



[System(false, true)]
public unsafe static class AB_Processor
{
    public static int[] Init()
        => [A.ComponentID, B.ComponentID];


    public static void Update(ArchetypeIterator* iter)
    {



    }


    public static void NarrowPhase(ArchetypeIterator* iter)
    {



    }


    public static void BroadPhase(ArchetypeIterator* iter)
    {



    }


    public static void PreRender(ArchetypeIterator* iter)
    {



    }


    public static void Render(ArchetypeIterator* iter)
    {



    }


    public static void PostRender(ArchetypeIterator* iter)
    {



    }
}


[Component]
public unsafe struct A
{
    public int i;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, A* component)
    {
        counter += 100;

        component->i = counter;


        Console.WriteLine("A INIT " + (nuint)component);
    }


    public static void Fin(int entityID, A* component)
    {
        Console.WriteLine("A FIN " + (nuint)component + " " + entityID + " " + component->i);



    }
}


[Component]
public unsafe struct B
{
    public short S;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, B* component)
    {
        counter += 20;

        component->S = (short)counter;

        Console.WriteLine("B INIT " + (nuint)component);


    }


    public static void Fin(int entityID, B* component)
    {
        Console.WriteLine("B FIN " + (nuint)component + " " + entityID + " " + component->S);



    }
}


[Component]
public unsafe struct C
{
    public byte b;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, C* component)
    {
        counter += 1;

        component->b = (byte)counter;

        Console.WriteLine("C INIT " + (nuint)component);


    }


    public static void Fin(int entityID, C* component)
    {
        Console.WriteLine("C FIN " + (nuint)component + " " + entityID + " " + component->b);



    }
}