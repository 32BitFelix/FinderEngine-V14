

using Core.LECSSimple;
using FinderIntrinsics;
using OpenTK.Mathematics;

namespace Core;





[Component]
public struct Transform
{
    // The scale of the entity
    public Vector4 Scale,

        // The orientation of the entity
        Rotation,

        // The position of the entity
        Translation;


    public static int ComponentID;
}


public unsafe static class TransformExt
{

    /// <summary>
    /// Returns an upwards pointing
    /// vector relative to the transformation.
    /// </summary>

    public static Vector4 Up(this Transform t)
        => Vector4.UnitY * t._createRotationMatrix();


    /// <summary>
    /// Returns a frontwards pointing
    /// vector relative to the transformation.
    /// </summary>

    public static Vector4 Front(this Transform t)
        => -Vector4.UnitZ * t._createRotationMatrix();


    /// <summary>
    /// Returns a rightwards pointing
    /// vector relative to the transformation.
    /// </summary>

    public static Vector4 Right(this Transform t)
        => Vector4.UnitX * t._createRotationMatrix();


    public static void SetGlobalScale(this Transform t, int eID, Vector4 nScale)
    {
        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
        {
            t.Scale = nScale;

            return;
        }


        Transform* ptr = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);

        t.Scale /= ptr->GetGlobalScale(parent);
    }


    public static Vector4 GetGlobalScale(this Transform t, int eID)
    {
        Vector4 scale = t.Scale;


        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
            return scale;


        Transform* ptr = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);

        scale *= ptr->GetGlobalScale(parent);


        return scale;
    }


    public static void SetGlobalRotation(this Transform t, int eID, Vector4 nRotation)
    {
        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
        {
            t.Scale = nRotation;

            return;
        }


        Transform* ptr = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);

        nRotation -= ptr->GetGlobalRotation(parent);


        t.Scale = nRotation;
    }


    public static Vector4 GetGlobalRotation(this Transform t, int eID)
    {
        Vector4 rotation = t.Rotation;


        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
            return rotation;


        Transform* ptr = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);

        rotation += ptr->GetGlobalRotation(parent);


        return rotation;
    }


    // Helper method for creating a
    // rotation matrix

    private static Matrix4 _createRotationMatrix(this Transform t)
    {
        // Result, xRot, yRot, zRot

        Matrix4* mats = stackalloc Matrix4[4];


        // Evaluate the x rotation

        mats[1].M11 = mats[1].M44 = 1f; // Set the identities

        // Do the rotation stuff

        {
            float xRot = MathHelper.DegreesToRadians(t.Rotation.X);

            mats[1].M22 = mats[1].M33 = MathF.Cos(xRot);

            mats[1].M23 = mats[1].M32 = MathF.Sin(xRot);

            mats[1].M23 *= -1.0f;
        }


        // Evaluate the y rotation

        mats[2].M22 = mats[2].M44 = 1.0f; // Set the identities

        // Do the rotation stuff

        {
            float yRot = MathHelper.DegreesToRadians(t.Rotation.Y);

            mats[2].M11 = mats[2].M33 = MathF.Cos(yRot);
        
            mats[2].M13 = mats[2].M31 = MathF.Sin(yRot);

            mats[2].M31 *= -1.0f;  
        }


        // Evaluate the z rotation matrix

        mats[3].M33 = mats[3].M44 = 1.0f; // Set the identities

        // Do the rotation stuff

        {
            float zRot = MathHelper.DegreesToRadians(t.Rotation.Z);

            mats[3].M11 = mats[3].M22 = MathF.Cos(zRot);

            mats[3].M12 = mats[3].M21 = MathF.Sin(zRot);

            mats[3].M12 *= -1.0f;
        }


        // Calculate the result

        mats[0] = mats[1] * mats[2] * mats[3];


        // Return the result

        return mats[0];
    }


    public static void SetGlobalTranslation(this Transform t, int eID, Vector4 nTranslation)
    {
        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
            t.Translation = nTranslation;


        Transform* ptr = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);

        nTranslation -= ptr->GetGlobalTranslation(parent);


        t.Translation = nTranslation;
    }


    public static Vector4 GetGlobalTranslation(this Transform t, int eID)
    {
        int parent = Finder.ShowParent(eID); 


        int comp = Transform.ComponentID;

        if(parent == 0 || !Finder.HasComponents(parent, &comp, 1))
            return t.Translation;


        Transform* pT = (Transform*)Finder.GetComponent(parent, Transform.ComponentID);
        
        return t.Translation * pT->GetGlobalScale(parent) * pT->_createRotationMatrix() + pT->GetGlobalTranslation(parent);
    }


    public static Matrix4 GetModelMatrix(this Transform t, int eID)
    {
        // Set the scale of the
        // model matrix

        Matrix4 mat = new();

        mat.Row0.X = t.Scale.X;

        mat.Row1.Y = t.Scale.Y;

        mat.Row2.Z = t.Scale.Z;

        mat.Row3.W = 1.0f;


        // Rotate the model matrix

        mat *= t._createRotationMatrix();


        // Translate the model matrix

        {
            Matrix4 transMat = Matrix4.Identity;

            Vector4 translation = t.GetGlobalTranslation(eID);


            transMat.Row3.X = translation.X;

            transMat.Row3.Y = translation.Y;

            transMat.Row3.Z = translation.Z;


            mat *= transMat;
        }


        // Return the model matrix

        return mat;
    }    


    public static Matrix4 GetViewMatrix(this Transform t, int eID)
    {
        Vector4 pos = t.GetGlobalTranslation(eID);

        Vector4 up = t.Up();

        Vector4 front = t.Front();


        return Matrix4.LookAt(pos.Xyz, pos.Xyz + front.Xyz, up.Xyz);
    }
}