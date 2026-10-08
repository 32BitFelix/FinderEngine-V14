

using OpenTK.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.LECSSimple;
using Core.MemoryManagement;
using Core.Algorithms;
using Core.Shimshek;
using System.Collections;

namespace Core.SAS2D;


[System(false, false)]
public unsafe static class BroadPhase2DSystem
{
    public static int[] Init()
        => [Collider2D.ComponentID, Transform.ComponentID]; 


    // The list of bounds, of each
    // valid entity

    private static EntityPoint* eP = CompactArray.Create<EntityPoint>(0);


    // The amount of colliders, that
    // have been deemed valid

    private static int validColliders;


    // If it is opposite to the
    // engine's IsOddFrame

    private static bool wasOddFrame;


    // If it is true, the narrow phase
    // will use the entity point array
    // to evaluate collision pairs

    private static bool isProcessed;


    // The broad phase collision detection will
    // be conducted as follows:
    //
    // The colliders will be represented as
    // bounding circles and capsules.
    // The edges of the bounds will be
    // projected to a line that is the average
    // of the x and y axis like so . . . 
    // |      / <- The line to project to
    // |     /
    // |    /
    // |   /
    // |  /
    // | /
    // |/
    // /
    // ________
    //
    // Then the two most extreme points of the
    // bounds relative to the line will be saved
    // to an array, that will be sorted
    //
    // Finally, the collision pairs will be
    // evaluated by sweeping through the sorted array

    public static void BroadPhase(ArchetypeIterator* iter)
    {
        // Sort the list of collision pairs

        if(wasOddFrame != Finder.IsOddFixedFrame)
        {
            wasOddFrame = Finder.IsOddFixedFrame;

            isProcessed = true;
        }


        // Reallocate the array, if it can't fit
        // the new bounds

        if(CompactArray.Length(eP) < (validColliders + iter->TotalLength() * 2))
            fixed(EntityPoint** ptr = &eP)
                CompactArray.Resize(ptr, validColliders + iter->TotalLength() * 2);


        // A vector representing the
        // general direction of the
        // line to project to

        const float averageX = 0.70710677f;

        const float averageY = 0.70710677f;


        // A normal vector representing
        // the perpendicular direction of the
        // line to project to

        const float perpX = -0.70710677f;

        const float perpY =  0.70710677f;


        // Iterate through each element
        // of the current iteration

        for(int i = 0; i < iter->Length(); i++)
        {
            int eID = iter->GetEntityID(i);

            if(eID < 2)
                continue;


            // Add a collision node

            if(validColliders >= CompactArray.Length(collisionNodes))
                fixed(CollisionNode** ptr = &collisionNodes)
                    CompactArray.Resize(ptr, validColliders * 2);

            {
                CollisionNode* node = &collisionNodes[validColliders];

                node->EntityID = eID;

                node->Parent = -1;

                node->Active = false;

                *(long*)&node->CorrectionForce ^= *(long*)&node->CorrectionForce;

                node->Collisions ^= node->Collisions;
            }


            // Get the components

            Transform* tran = (Transform*)iter->GetComponent(i, Transform.ComponentID);

            Collider2D* col = (Collider2D*)iter->GetComponent(i, Collider2D.ComponentID);

            RigidBody2D* rb = (RigidBody2D*)Finder.GetComponent(eID, RigidBody2D.ComponentID);


            // A multiplier that changes based on the direction
            // of the current rigidbody's linear trajectory.
            // If it is below the line to project to, it's -1,
            // otherwise it's 1

            sbyte mult = 0;

            {
                Vector2 other = rb != null ? -rb->TranslationalVelocity : (1, 0);

                float cross = PhysUtil.CrossProduct((perpX, perpY), other);
            
                mult = (sbyte)(0xFF * (*(uint*)&cross >> 31));

                mult |= 1;
            }


            // Get the radius of the
            // rotund part of the capsule

            float scale = getLargestScale(tran->Scale.Xy);

            scale = col->Radius > scale ? col->Radius : scale;


            scale *= mult;


            // The two extremes of the capsule
            // projected from the line

            Vector2 fP = new();

            Vector2 sP = new();


            // Now comes the exciting part.
            // The positions of the bounds
            // will be calculated. The body
            // of the capsule will be added
            // to the highest point too, if
            // the current entity has a rigidbody

            {
                Vector2* firstPoint = mult < 0 ? &fP : &sP;

                Vector2* secondPoint = mult < 0 ? &sP : &fP;


                *firstPoint = tran->Translation.Xy;

                *firstPoint += new Vector2(averageX, averageY) * scale;


                *secondPoint = tran->Translation.Xy;

                *secondPoint -= rb != null ? rb->TranslationalVelocity : (0, 0);

                *secondPoint += new Vector2(-averageX, -averageY) * scale;
            }


            /*if(rb != null)
            {
                Transform* firTran = (Transform*)Finder.GetComponent(rb->firstEnt, Transform.ComponentID);

                firTran->Translation.Xy = fP;


                Transform* secTran = (Transform*)Finder.GetComponent(rb->secEnt, Transform.ComponentID);

                secTran->Translation.Xy = sP;
            }*/


            // Rotate the two extremes

            fP = RotateVector(fP, MathHelper.DegreesToRadians(-45));
            
            sP = RotateVector(sP, MathHelper.DegreesToRadians(-45));


            // Add the extremes to the entity point array

            eP[validColliders * 2].Entity = validColliders;

            eP[validColliders * 2].Point = fP.X;


            eP[validColliders * 2 + 1].Entity = validColliders;

            eP[validColliders * 2 + 1].Point = sP.X;


            // Increment the amount of
            // valid colliders

            validColliders++;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 RotateVector(Vector2 vec, float angle)
    {
        Vector2 result;

        result.X = vec.X * MathF.Cos(angle) - vec.Y * MathF.Sin(angle);

        result.Y = vec.X * MathF.Sin(angle) + vec.Y * MathF.Cos(angle);

        return result;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float getLargestScale(Vector2 scale)
        => scale.X > scale.Y ? scale.X : scale.Y;


    // Represents a collider in the
    // collision graph

    private struct CollisionNode
    {
        // The ID of the entity
        // related to this node

        public int EntityID;


        // The index of the node,
        // that is higher than this one

        public int Parent;

        public bool Active;


        // The sum of all collisions,
        // which will be averaged and
        // added to the position of the
        // related entity

        public Vector2 CorrectionForce;

        public int Collisions;
    }


    // The collision graph

    private static CollisionNode* collisionNodes = CompactArray.Create<CollisionNode>(1);


    // The narrow phase collision detection
    // will be conducted as follows:
    //
    // First a graph will be set up, with
    // all bounds being parented based on their overlaps.
    //
    // Then the nodes will be checked for collisions
    // with their parents

    public static void NarrowPhase(ArchetypeIterator* iter)
    {
        // Skip this for now,
        // if the array holding the
        // bounds hasn't been prepared yet

        if(!isProcessed)
            return;


        // Sort the bounds

        Sorting.RadixSort(eP, validColliders * 2);


        // The index of the topmost node

        int topMostNode = -1;

        // Iterate through each bound

        for(int i = 0; i < validColliders * 2; i++)
        {
            CollisionNode* node = &collisionNodes[eP[i].Entity];


            // Flip the state of the node

            node->Active = !node->Active;


            // The parent of the node will be set,
            // if the current node is the beginning
            // of a bound

            node->Parent = node->Active ? topMostNode : node->Parent;


            // The topmost node will be set to the index of the parent
            // of the current node, if the current node is
            // an end of a bound, that is directly connected
            // to the beginning end of the same bound.
            // If the current node simply is a beginning end
            // of a bound, then the topmost node will be set
            // to it's index

            {
                int first = !node->Active ? topMostNode : eP[i].Entity;

                topMostNode = eP[i].Entity == topMostNode ? collisionNodes[eP[i].Entity].Parent : first;
            }
        }


        // Iterate through each node

        for(int n = 0; n < validColliders; n++)
        {
            // Iterate through each node,
            // that is ordered higher than
            // the current node

            int parent = collisionNodes[n].Parent;

            while(parent >= 0)
            {
                checkCollision(n, parent);

                parent = collisionNodes[parent].Parent;
            }
        }


        // Reset the state of the
        // broadphase system

        validColliders ^= validColliders;

        isProcessed = !isProcessed;
    }


    private static void checkCollision(int entA, int entB)
    {
        // Get the entity IDs of each node

        int aID = collisionNodes[entA].EntityID;

        int bID = collisionNodes[entB].EntityID;


        // Get the references of the nodes' transformations

        Transform* aTran = (Transform*)Finder.GetComponent(aID, Transform.ComponentID);

        Transform* bTran = (Transform*)Finder.GetComponent(bID, Transform.ComponentID);


        // Get the references of the nodes' colliders

        Collider2D* aCol = (Collider2D*)Finder.GetComponent(aID, Collider2D.ComponentID);

        Collider2D* bCol = (Collider2D*)Finder.GetComponent(bID, Collider2D.ComponentID);


        // A normal representing the
        // rough direction, that both
        // colliders will be resolved towards

        Vector2 roughResolveNormal;


        Vector2 aPos;

        Vector2 bPos;

        float* factors = stackalloc float[2];


        {
            // Get the references of the rigidbodies
            // of both nodes

            RigidBody2D* aRB = (RigidBody2D*)Finder.GetComponent(aID, RigidBody2D.ComponentID);

            RigidBody2D* bRB = (RigidBody2D*)Finder.GetComponent(bID, RigidBody2D.ComponentID);


            // Evaluate the trajectory of
            // the first collider

            Vector2 lineEndA = aTran->GetGlobalTranslation(aID).Xy;

            Vector2 lineStartA = aRB != null ? aRB->LinearVelocity + aRB->TranslationalVelocity : (0, 0);

            lineStartA = lineEndA - lineStartA;


            // Evaluate the trajectory of
            // the second collider

            Vector2 lineEndB = bTran->GetGlobalTranslation(bID).Xy;

            Vector2 lineStartB = bRB != null ? bRB->LinearVelocity + bRB->TranslationalVelocity : (0, 0);

            lineStartB = lineEndB - lineStartB;


            // Calculate the rough resolve normal

            roughResolveNormal = lineStartB - lineStartA;

            roughResolveNormal.Normalize();

            //PhysUtil.NormalizeFast(ref roughResolveNormal);

#pragma warning disable CS1718

            if(roughResolveNormal.X != roughResolveNormal.X)
                roughResolveNormal = (0, 1);

#pragma warning restore CS1718


            // Evaluate, where the trajectories would
            // be touching

            PhysUtil.LSLSDistance(lineStartA, lineEndA, lineStartB, lineEndB, factors);


            aPos = lineStartA + (lineEndA - lineStartA) * factors[0];

            bPos = lineStartB + (lineEndB - lineStartB) * factors[1];
        }


        // Prematurely end the method,
        // if the trajectories of the colliders
        // don't touch

        {
            float distanceSquared = Vector2.DistanceSquared(aPos, bPos);


            float circleSumSquared = 0;


            float scale = getLargestScale(aTran->Scale.Xy);

            scale = scale < aCol->Radius ? aCol->Radius : scale;

            circleSumSquared += scale;


            scale = getLargestScale(bTran->Scale.Xy);

            scale = scale < bCol->Radius ? bCol->Radius : scale;

            circleSumSquared += scale;


            circleSumSquared *= circleSumSquared;


            if(distanceSquared > circleSumSquared)
                return;
        }


        // Move the entities to where they would be,
        // if there were to be a collision

        




        //Console.WriteLine(roughResolveNormal);



    }


    // TODO: Add an SAP algorithm,
    // to speed up the checks between
    // the triangles of both colliders

    // Conducts a direct collision test
    // between the two given colliders

    private static bool collisionCheck(Collider2D* colA, Collider2D* colB,
        Transform* tranA, Transform* tranB, int aID, int bID, Vector2 rrNormal)
    {
        PhysUtil.Triangle triA;

        PhysUtil.Triangle triB;


        // Iterate through the triangles of
        // collider A

        for(int i = 0; i < CompactArray.Length(colA->Triangles); i++)
        {
            triA.A = colA->Vertices[colA->Triangles[i].A];

            triA.B = colA->Vertices[colA->Triangles[i].B];

            triA.C = colA->Vertices[colA->Triangles[i].C];


            PhysUtil.TransformTriangle(ref triA, tranA->GetModelMatrix(aID));


            // Iterate through the triangles
            // of collider B

            for(int j = 0; j < CompactArray.Length(colB->Triangles); j++)
            {
                triB.A = colB->Vertices[colB->Triangles[i].A];

                triB.B = colB->Vertices[colB->Triangles[i].B];
            
                triB.C = colB->Vertices[colB->Triangles[i].C];


                PhysUtil.TransformTriangle(ref triB, tranB->GetModelMatrix(bID));



            }
        }



        return false;
    }
}


[System(false, false)]
public unsafe static class Rigidbody2DSystem
{
    public static int[] Init()
        => [Transform.ComponentID, RigidBody2D.ComponentID];

    
    public static void BroadPhase(ArchetypeIterator* iter)
    {
        // Iterate through each entity of the
        // current iteration

        for(int i = 0; i < iter->Length(); i++)
        {
            // Skip the current entity,
            // if it is invalid

            if(iter->GetEntityID(i) < 2)
                continue;


            // Get the references to the
            // transformation and rigidbody
            // component of the current entity

            Transform* tran = (Transform*)iter->GetComponent(i, Transform.ComponentID);

            RigidBody2D* rb = (RigidBody2D*)iter->GetComponent(i, RigidBody2D.ComponentID);


            // Evaluate the translational
            // velocity of the rigidbody

            rb->TranslationalVelocity = tran->Translation.Xy - rb->LastTranslation;

            // Evaluate the velocity of the
            // gravity acting on the rigidbody

            rb->LinearVelocity += RigidBody2D.WorldGravity * rb->Mass * Finder.FixedDeltaTime;

            // Set the current position as the
            // last translation of the rigidbody

            rb->LastTranslation = tran->Translation.Xy;

            // Finally, move the rigidbody
            // based on the force acting on it

            tran->Translation.Xy += (rb->TranslationalVelocity + rb->LinearVelocity) * Finder.FixedDeltaTime;
        }


    }
}


// The rigidbody2D component keeps
// track of the physical forces
// influencing an entity

[Component]
public unsafe struct RigidBody2D
{


    public static int ComponentID;


    public static void Init(int eID, RigidBody2D* rb)
    {
        *rb = default;


        int* comps = stackalloc int[2];

        comps[0] = Transform.ComponentID;

        comps[1] = Sprite.ComponentID;


        Finder.CreateEntities(&rb->firstEnt, 1);

        Finder.AddComponents(rb->firstEnt, comps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(rb->firstEnt, Transform.ComponentID);

            tran->Scale = (0.1f, 0.1f, 1, 0);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, 0, 3, 0);
        }        

        {
            Sprite* spr = (Sprite*)Finder.GetComponent(rb->firstEnt, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            int texCol = 255 | 255 << 24;

            spr->Texture.Create((byte*)&texCol, 1, 1, false);
        }


        Finder.CreateEntities(&rb->secEnt, 1);

        Finder.AddComponents(rb->secEnt, comps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(rb->secEnt, Transform.ComponentID);

            tran->Scale = (0.1f, 0.1f, 1, 0);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, 0, 3, 0);
        }        

        {
            Sprite* spr = (Sprite*)Finder.GetComponent(rb->secEnt, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            int texCol = 255 << 8 | (255 << 24);

            spr->Texture.Create((byte*)&texCol, 1, 1, false);
        }

    }


    // The force of gravity that acts
    // upon all rigidbodies

    public static Vector2 WorldGravity = (0, -9.81f);


    // The velocities of the entity's
    // X and Y position

    public Vector2 TranslationalVelocity;

    // The velocity of the entity's
    // Z axis

    public float RotationalVelocity;

    // The linear velocity of the
    // collider's position

    public Vector2 LinearVelocity;


    // The translational position
    // of the rigidbody from the
    // last update

    public Vector2 LastTranslation;


    // The mass of the
    // rigidbody in KGs

    public float Mass;


    // The Id of the material,
    // that the rigidbody borrows
    // the physical attributes from

    public int MaterialID;


    public int firstEnt, secEnt;


    // Flags, that can define unique
    // behaviour for a rigidbody

    public RB2DFlag Flags;
}


[Flags]
public enum RB2DFlag : byte
{
    None = 0,

    FreezeRot = 1,

    FreezeX = 2,

    FreezeY = 4
}


// Bestows physical attributes,
// that multiple rigidbodies can
// borrow from

public unsafe struct RigidBodyMaterial2D
{
    // Type initialiser

    static RigidBodyMaterial2D()
    {
        materials = CompactArray.Create<RigidBodyMaterial2D>(0);


    }


    // The list of currently active materials

    public static RigidBodyMaterial2D* materials;


    // Adds the material to the material list
    // and returns the index, that the material
    // was saved at

    public static int AddMaterial(RigidBodyMaterial2D mat)
    {   
        // Try to find a free index

        int i = CompactArray.Length(materials) - 1;

        for(; i > -1; i--)
        {
            // Skip this iteration,
            // if it isn't free

            if(materials[i].Restitution >= 0.0f)
                continue;


            // Return the index, that the
            // new material is saved at

            materials[i] = mat;

            return i;
        }


        // Resize the array to fit the
        // new material

        i = CompactArray.Length(materials);

        fixed(RigidBodyMaterial2D** ptr = &materials)
            CompactArray.Resize(ptr, i + 1);

        materials[i] = mat;


        return i;
    }


    // Removes the material, that was
    // saved at the given index

    public static void RemoveMaterial(int matID)
    {
        // Prematurely end the method,
        // if the matID goes beyond the
        // length of the materials list

        if(matID >= CompactArray.Length(materials))
            return;


        // Do some eco disposal

        materials[matID].Restitution = -1.0f;
    }


    // The restitution of energy
    // of the rigidbody material

    public float Restitution;


    // The surface roughness of
    // the rigidbody material

    public float Roughness;
}


// The collider2D component makes
// sure, that entities don't intersect
// with eachother

[Component]
public unsafe struct Collider2D
{
    public static int ComponentID;


    public static void Init(int eID, Collider2D* col)
    {
        // Reset the fields of the
        // collider


        col->Layer ^= col->Layer;

        col->IgnoreLayers ^= col->IgnoreLayers;


        col->Vertices = null;

        col->Triangles = null;


        col->CollidingWith = CompactArray.Create<int>(0);


        col->MaterialID ^= col->MaterialID;


        *(int*)&col->Radius ^= *(int*)&col->Radius;


        col->Flags ^= col->Flags;
    }


    public static void Fin(int eID, Collider2D* col)
    {
        // Dispose of the unmanaged
        // resources 

        if(col->Vertices != null)
        {
            CompactArray.Delete(col->Vertices);

            CompactArray.Delete(col->Triangles);
        }


        // Free the list, that kept track
        // of the entities, that the
        // collider collided with

        CompactArray.Delete(col->CollidingWith);
    }


    // The collision layers,
    // that the collider is part of

    public long Layer;

    // A mask representing the
    // layers, that the collider shall ignore

    public long IgnoreLayers;


    // The vertices representing
    // the shape of the collider

    public Vector4* Vertices;

    // The triangulates triangulated
    // off of the given polygon

    public TriangleRecord* Triangles;


    // Gets a list of references
    // to entities, that the
    // collider collided with

    public int* CollidingWith;


    // The ID of the material,
    // that the collider borrows
    // the aspects from

    public int MaterialID;


    // The radius of the circle
    // surrounding the collider's shape

    public float Radius;


    // Flags that can define
    // unique behaviour for a collider

    public Col2DFlag Flags;
}


// Defines unique behaviour,
// that a collider can have

[Flags]
public enum Col2DFlag : byte
{
    // There is no unique behaviour
    // for the affected collider

    None = 0,

    // The affected collider
    // cannot be moved

    Static = 1,

    // The affected collider
    // only calls collision events

    Trigger = 2,
}

public unsafe static class Collider2DExt
{
    // A helper method for triangulating
    // the given list of vertices (compact array)

    public static void SetPolygon(this ref Collider2D col, Vector2* vertices)
    {
        // Free the previous shape,
        // if there is any

        if(col.Vertices != null)
        {
            CompactArray.Delete(col.Triangles);

            CompactArray.Delete(col.Vertices);
        }


        // Allocate the vertices array
        // for the collider, copy
        // the vertices over and see,
        // if the vertices are laid
        // in a counter clockwise fashion

        float allSum = 0;

        col.Vertices = CompactArray.Create<Vector4>(CompactArray.Length(vertices));

        for(int i = CompactArray.Length(vertices) - 1; i > -1; i--)
        {
            // Convert the vertex to a
            // four dimensional vector

            col.Vertices[i] = new Vector4(vertices[i]);


            // Add the vertex to the sum
            // of the polygon

            {
                int next = (i + 1) % CompactArray.Length(vertices);

                allSum += (vertices[i].X - vertices[next].X) * (vertices[i].Y + vertices[next].Y);
            }


            // Get the potential radius of the
            // circle surrounding the polygon

            {
                float len = vertices[i].Length;

                if(len > col.Radius)
                    col.Radius = len;
            }
        }


        // Triangulate the given list of vertices

        col.Triangles = PhysUtil.TriangulatePolygon2D(vertices, allSum >= 0);
    }


    // Adds the given layer to the collider

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddLayer(this Collider2D col, byte layer)
        => col.Layer |= (long)1 << layer;


    // Removes the given layer from the collider

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveLayer(this Collider2D col, byte layer)
        => col.Layer ^= (long)1 << layer;


    // Adds the given ignore layer to the collider

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddIgnoreLayer(this Collider2D col, byte ignoreLayer)
        => col.Layer |= (long)1 << ignoreLayer;


    // Removes the given  ignore layer from the collider

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveIgnoreLayer(this Collider2D col, byte ignoreLayer)
        => col.Layer ^= (long)1 << ignoreLayer;
}


// Bestows physical attributes,
// that multiple colliders can
// borrow from

public unsafe struct ColliderMaterial2D
{
    // Type initialiser

    static ColliderMaterial2D()
    {
        materials = CompactArray.Create<ColliderMaterial2D>(0);


    }


    // The list of currently active materials

    public static ColliderMaterial2D* materials;


    // Adds the material to the material list
    // and returns the index, that the material
    // was saved at

    public static int AddMaterial(ColliderMaterial2D mat)
    {   
        // Try to find a free index

        int i = CompactArray.Length(materials) - 1;

        for(; i > -1; i--)
        {
            // Skip this iteration,
            // if it isn't free

            if(materials[i].Used)
                continue;


            // Return the index, that the
            // new material is saved at

            materials[i] = mat;

            materials[i].Used = !materials[i].Used;

            return i;
        }


        // Resize the array to fit the
        // new material

        i = CompactArray.Length(materials);

        fixed(ColliderMaterial2D** ptr = &materials)
            CompactArray.Resize(ptr, i + 1);

        materials[i] = mat;

        materials[i].Used = !materials[i].Used;


        return i;
    }


    // Removes the material, that was
    // saved at the given index

    public static void RemoveMaterial(int matID)
    {
        // Prematurely end the method,
        // if the matID goes beyond the
        // length of the materials list

        if(matID >= CompactArray.Length(materials))
            return;


        // Make sure, that the material
        // is flagged as unused

        materials[matID].Used = !materials[matID].Used;
    }


    // Indicates, if the material
    // is still is in use

    public bool Used;


    // Callbacks based on specific
    // collision events

    public delegate*<Collision2DInfo, void> OnEnter, OnStay, OnExit;   
}


// Bestows the information
// of a collision

public struct Collision2DInfo
{
    // The normal of the collision

    public Vector2 Normal;

    // The depth of the collision

    public float Depth;

    // The entity ID of the
    // hit collider and the
    // collider hitting it

    public int Self, Other;
}


// Stores the indices of the
// vertices of a triangle

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct TriangleRecord
{
    // The indices of the
    // vertices representing
    // a triangle

    public int A, B, C;

    // The radius of the
    // circle surrounding
    // the triangle

    public float Radius;
}


// Helper class containing utilities
// for the physics simulation

public unsafe static class PhysUtil
{
    // A structure, that simply
    // represents a polygon with
    // three vertices: a triangle

    public struct Triangle
    {
        // The individual vertices
        // of the triangle

        public Vector4 A, B, C;
    }


    // Transforms the given triangle
    // by the given tranformation matrix

    public static void TransformTriangle(ref Triangle tri, Matrix4 TransformMatrix)
    {
        tri.A *= TransformMatrix;

        tri.B *= TransformMatrix;

        tri.C *= TransformMatrix;
    }


    // Checks the collision between
    // two triangles and returns the
    // normal and depth of the penetration

    public static void TriangleIntersectsTriangle2D(ref Triangle a, ref Triangle b,
            out float depth, out Vector2 normal)
    {
        depth = 0;

        normal = (0, 0);

    
        // Edge aA aB against triangle B

        {
            Vector2 axis = a.B.Xy - a.A.Xy;

            axis = (-axis.Y, axis.X);

            axis.Normalize();


            Vector2 aA_To_bA = b.A.Xy - a.A.Xy;

            float projA = Vector2.Dot(aA_To_bA, axis);


            Vector2 aA_To_bB = b.B.Xy - a.A.Xy;

            float projB = Vector2.Dot(aA_To_bB, axis);


            Vector2 aA_To_bC = b.C.Xy - a.A.Xy;

            float projC = Vector2.Dot(aA_To_bC, axis);


            Console.WriteLine(projA + " " + projB + " " + projC);
        }
    }


    // Takes a list of vertices (Compact array)
    // and returns a list of triangles (Compact array)
    // indexing from the vertices list

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static TriangleRecord* TriangulatePolygon2D(Vector2* vertices, bool isCounterClockwise)
    {
        // The list of triangles to return

        TriangleRecord* tris;


        // Check for edge cases

        switch(CompactArray.Length(vertices))
        {
            // No vertices

            case 0:
                return CompactArray.Create<TriangleRecord>(0);

            // One vertex

            case 1:
                tris = CompactArray.Create<TriangleRecord>(1);

                tris[0] = new TriangleRecord()
                {
                    A = 0,

                    B = 0,

                    C = 0
                };

                return tris;

            // Two vertices
            
            case 2:
                tris = CompactArray.Create<TriangleRecord>(1);

                tris[0] = new TriangleRecord()
                {
                    A = 0,

                    B = 1,

                    C = 0
                };

                return tris;
        }


        // Evaluate the theoretical amount of triangles,
        // that the given polygon has and allocate the
        // triangle array according to the amount

        int triangleCount = CompactArray.Length(vertices) - 2;

        tris = CompactArray.Create<TriangleRecord>(triangleCount);


        // Allocate the array, that'll keep track
        // of the ignored vertices

        bool* ignoreVertices = stackalloc bool[CompactArray.Length(vertices)];


        // Iterate through each possible triangle

        for(int t = triangleCount - 1; t > -1; t--)
        {
            // The current triangle

            TriangleRecord* tri = &tris[t];


            // Set vertex A to zero

            tri->A ^= tri->A;


            // Set the radius to zero

            *(int*)&tris->Radius ^= *(int*)&tris->Radius;


            // A restart point, in case
            // the triangle is a failure

            restart:


            // Iterate through each vertex

            for(int v = tri->A; v < CompactArray.Length(vertices); v++)
            {
                // Skip this vertex, if it is ingored

                if(ignoreVertices[v])
                    continue;


                // Save the current vertex
                // as the a point of the triangle

                tri->A = v;

                break;
            }


            // Iterate through each vertex

            for(int v = (tri->A + 1) % CompactArray.Length(vertices); v < CompactArray.Length(vertices); v++)
            {
                // Skip this vertex, if it is ingored

                if(ignoreVertices[v])
                    continue;


                // Save the current vertex
                // as the b point of the triangle

                tri->B = v;

                break;
            }


            // Iterate through each vertex

            for(int v = (tri->B + 1) % CompactArray.Length(vertices); v < CompactArray.Length(vertices); v++)
            {
                // Skip this vertex, if it is ingored

                if(ignoreVertices[v])
                    continue;


                // Save the current vertex
                // as the c point of the triangle

                tri->C = v;

                break;
            }



            // See, if the evaluated triangle
            // is within the bounds of the polygon

            {
                // Calculate the difference
                // between vertex b and a

                Vector2 bToA = vertices[tri->B] - vertices[tri->A];

                // Calculate the difference
                // between vertex c and a
                
                Vector2 cToA = vertices[tri->C] - vertices[tri->A];


                // Evaluate the crossproduct
                // between the two edges

                float crossProduct = CrossProduct(cToA, bToA);

                // Evaluate, if the crossproduct
                // needs to be negated for the sake
                // of backwards winding polygons

                sbyte mult = (sbyte)(0xFF * *(byte*)&isCounterClockwise);

                mult |= 1;

                crossProduct *= mult;


                // If the crossproduct is
                // negative, this means,
                // that the triangle is beyond
                // the polygon's bounds

                if(crossProduct < 0.0f)
                {
                    tri->A++;
                    
                    tri->A %= CompactArray.Length(vertices);

                    goto restart;
                }
            }


            // Check, if any vertex is within the triangle

            {
                // Evaluate the volume of the triangle

                float triArea = HeronsFormula(vertices[tri->A], vertices[tri->B], vertices[tri->C]);


                for(int v = 0; v < CompactArray.Length(vertices); v++)
                {
                    if(v == tri->A || v == tri->B || v == tri->C)
                        continue;


                    // Evaluate the volume of the "quad"
                    // consisting of the current vertex
                    // and the vertices of the triangle

                    float area = HeronsFormula(vertices[v], vertices[tri->A], vertices[tri->B]);

                    area += HeronsFormula(vertices[v], vertices[tri->B], vertices[tri->C]);

                    area += HeronsFormula(vertices[v], vertices[tri->C], vertices[tri->A]);


                    if(triArea <= area)
                        continue;


                    // If the area of the "quad"
                    // ever happens to be less than or
                    // equal to the triangle's volume,
                    // this means, that the current
                    // vertex is within the current triangle
                    
                    tri->A++;
                    
                    tri->A %= CompactArray.Length(vertices);

                    goto restart;
                }
            }


            // Add the middle vertex of the
            // triangle to the ignore vetrex array

            ignoreVertices[tri->B] = !ignoreVertices[tri->B];


            // Evaluate the center of mass of the triangle

            Vector2 centerOfMass = vertices[tri->A] + vertices[tri->B] + vertices[tri->C] * (1.0f / 3);


            // VERTEX A

            {
                float dist = Vector2.Distance(centerOfMass, vertices[tri->A]);

                if(dist > tri->Radius)
                    tris->Radius = dist;
            }

            // VERTEX B

            {
                float dist = Vector2.Distance(centerOfMass, vertices[tri->B]);

                if(dist > tri->Radius)
                    tris->Radius = dist;
            }

            // VERTEX C

            {
                float dist = Vector2.Distance(centerOfMass, vertices[tri->C]);

                if(dist > tri->Radius)
                    tris->Radius = dist;
            }
        }


        // Return the list of triangles

        return tris;
    }


    // Calculate crossproduct
    // of two vectors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CrossProduct(Vector2 value1, Vector2 value2)
        => value1.X * value2.Y - value1.Y * value2.X;


    // An old formula for evaluating
    // the area of a triangle

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float HeronsFormula(Vector2 a, Vector2 b, Vector2 c)
        => float.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y));


    // Evaluate the two closest points
    // between two lines 

    public static void LSLSDistance(Vector2 lineStartA, Vector2 lineEndA,
        Vector2 lineStartB, Vector2 lineEndB, float* factors)
    {
        /*// Evaluate the closest
        // endpoint of line A to line B 

        Vector2 bestA = (0, 0);

        {
            float distA = Vector2.DistanceSquared(lineStartA, lineStartB);

                distA += Vector2.DistanceSquared(lineStartA, lineEndB);

            float distB = Vector2.DistanceSquared(lineEndA, lineStartB);

                distB += Vector2.DistanceSquared(lineEndA, lineEndB);

            bestA = distA <= distB ? lineStartA : lineEndA;
        }


        // Calculate a point on line segment B nearest to the
        // best potential endpoint on line segment A

        LSPointDistance(lineStartB, lineEndB, bestA, &factors[1]);


        // Calculate the point on line B,
        // that is closest to lineA

        Vector2 bestB = lineStartB + (lineEndB - lineStartB) * factors[1];


        // Now do the same for line segment A

        LSPointDistance(lineStartA, lineEndA, bestB, factors);*/



    }


    // Evaluate the closest point
    // on the line to the given point

    public static void LSPointDistance(Vector2 lineStart, Vector2 lineEnd, Vector2 point, float* factor)
    {
        Vector2 ab = lineEnd - lineStart;

        Vector2 ap = point - lineStart;


        // Evaluate the factor

        float proj = Vector2.Dot(ap, ab);

        *factor = proj * float.ReciprocalEstimate(ab.LengthSquared);


        // Set the factor to zero,
        // if it is an invalid value

        if(*factor != *factor)
            *factor = 0.0f;


        // Clamp the factor between
        // the range of 0.0f and 1.0f

        if(*factor < 0.0f)
            *factor = 0.0f;

        if(*factor > 1.0f)
            *factor = 1.0f;
    }


    // Returns the length of the vector
    // with a fast approximation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LengthFast(Vector2 vec)
        => float.ReciprocalSqrtEstimate(vec.X * vec.X + vec.Y * vec.Y);


    // Normalizes the given vector
    // with a fast approximation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NormalizeFast(ref Vector2 vec)
    {
        float len = float.ReciprocalSqrtEstimate(vec.X * vec.X + vec.Y * vec.Y);

        vec.X *= len;

        vec.X *= len;
    }


    // Evaluates the factors between both
    // lines, where the intersection takes place

    public static void GetClosestLineIntersections(Vector2* a, Vector2* b, float* aFactor, float* bFactor)
    {
        // Evaluate the denominators

        float aDen = (b[1].X - b[0].X) * (b[0].Y - a[0].Y) - (b[1].Y - b[0].Y) * (b[0].X - a[0].X);

        float bDen = (b[1].X - b[0].X) * (a[1].Y - a[0].Y) - (b[1].Y - b[0].Y) * (a[1].X - a[0].X);

        float cDen = (a[1].X - a[0].X) * (b[0].Y - a[0].Y) - (a[1].Y - a[0].Y) * (b[0].X - a[0].X);


        // Evaluate the inverse
        // of the b denominator

        float invBDen = float.ReciprocalEstimate(bDen);


        // Calculate the factors

        *aFactor = aDen * invBDen;

        *bFactor = cDen * invBDen;
    }
}