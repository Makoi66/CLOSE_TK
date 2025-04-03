using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CLOSE_TK
{
    class Camera
    {
        private float SPEED = 8f;
        private int SCREENWIDTH;
        private int SCREENHEIGHT;
        private float SENSITIVITY = 100f;

        private float pitch;
        private float yaw = -90.0f;
        private bool firstMove = true;
        public Vector2 lastPos;

        public Vector3 position;

        Vector3 up = Vector3.UnitY;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;

        private float Clamp(float x)
        {
            if (x <= -89.0f) return -89.0f;
            if (x >= 89.0f) return 89.0f;
            return x;
        }

        //public void UpdateScreenSize(int width, int height)
        //{
        //    SCREENWIDTH = width;
        //    SCREENWIDTH = height;
        //}

        private void UpdateVectors()
        {
            front.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) *
                MathF.Cos(MathHelper.DegreesToRadians(yaw));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            front.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) *
                MathF.Sin(MathHelper.DegreesToRadians(yaw));

            front = Vector3.Normalize(front);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public Camera(int width, int height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(position, position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                (float)SCREENWIDTH / SCREENHEIGHT, 0.1f, 100f);
        }


        public void InputController(KeyboardState input,
            MouseState mouse, FrameEventArgs e)
        {
            if (input.IsKeyDown(Keys.W))
            {
                position += front * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.A))
            {
                position -= right * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.S))
            {
                position -= front * SPEED * (float)e.Time;
            }
            if (input.IsKeyDown(Keys.D))
            {
                position += right * SPEED * (float)e.Time;
            }

            if (firstMove)
            {
                lastPos = new Vector2(position.X, position.Y);
                firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - lastPos.X;
                var deltaY = mouse.Y - lastPos.Y;

                lastPos = new Vector2(mouse.X, mouse.Y);

                yaw += deltaX * SENSITIVITY * 0.001f;
                pitch -= deltaY * SENSITIVITY * 0.001f;
                pitch = Clamp(pitch); 
            }

            UpdateVectors();
        }
        public void Update(KeyboardState input,
            MouseState mouse, FrameEventArgs e)
        {
            InputController(input, mouse, e);
        }
    }
}
