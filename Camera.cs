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
        private float boost = 1f;
        private int SCREENWIDTH;
        private int SCREENHEIGHT;
        private float SENSITIVITY = 100f;

        private float pitch;
        private float yaw = -90.0f;
        public bool firstMove = true;
        public Vector2 lastPos;

        public Vector3 Position;

        Vector3 up = Vector3.UnitY;
        Vector3 front = -Vector3.UnitZ;
        Vector3 right = Vector3.UnitX;

        private float Clamp(float x)
        {
            if (x <= -89.0f) return -89.0f;
            if (x >= 89.0f) return 89.0f;
            return x;
        }

        public void UpdateScreenSize(int width, int height)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
        }

        private void UpdateVectors()
        {
            Vector3 tempFront;
            tempFront.X = MathF.Cos(MathHelper.DegreesToRadians(pitch)) *
                MathF.Cos(MathHelper.DegreesToRadians(yaw));
            tempFront.Y = MathF.Sin(MathHelper.DegreesToRadians(pitch));
            tempFront.Z = MathF.Cos(MathHelper.DegreesToRadians(pitch)) *
                MathF.Sin(MathHelper.DegreesToRadians(yaw));
            front = Vector3.Normalize(tempFront);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        public Camera(int width, int height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.Position = position;

            UpdateVectors();
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            float aspectRatio = (SCREENHEIGHT == 0) ? 1.0f : 
                (float)SCREENWIDTH / SCREENHEIGHT;
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                aspectRatio, 0.1f, 200f);
        }


        public void InputController(KeyboardState input,
            MouseState mouse, FrameEventArgs e, out Vector2 currentPos)
        {
            currentPos = new Vector2(mouse.X, mouse.Y);

            float time = (float)e.Time;
            if (input.IsKeyDown(Keys.LeftControl)) boost = 2.5f;
            if (input.IsKeyDown(Keys.W))
            {
                Position += front * SPEED * time * boost;
            }
            if (input.IsKeyDown(Keys.A))
            {
                Position -= right * SPEED * time * boost;
            }
            if (input.IsKeyDown(Keys.S))
            {
                Position -= front * SPEED * time * boost;
            }
            if (input.IsKeyDown(Keys.D))
            {
                Position += right * SPEED * time * boost;
            }
            if (input.IsKeyDown(Keys.Space))
            {
                Position += Vector3.UnitY * SPEED * time;
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                Position -= Vector3.UnitY * SPEED * time;
            }

            boost = 1f;

            if(Position.Y < 0.2f)
            {
                Position.Y = 0.2f;
            }

            var deltaX = 0.0f;
            var deltaY = 0.0f;

            if (firstMove)
            {
                lastPos = currentPos;
                firstMove = false;
            }
            else
            {
                deltaX = currentPos.X - lastPos.X;
                deltaY = currentPos.Y - lastPos.Y;

                yaw += deltaX * SENSITIVITY * 0.001f;
                pitch -= deltaY * SENSITIVITY * 0.001f;
                pitch = Clamp(pitch);

                lastPos = currentPos;
            }

            UpdateVectors();
        }

        public void Update(KeyboardState input,
            MouseState mouse, FrameEventArgs e, out Vector2 newLastPos)
        {
            newLastPos = new Vector2(mouse.X, mouse.Y);
            InputController(input, mouse, e, out newLastPos);
        }
    }
}
