using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GAME_1
{
    public class CActor
    {
        public int X, Y;
        public int W, H;
        public Bitmap img;
        public List<Bitmap> walkRight = new List<Bitmap>();
        public List<Bitmap> walkLeft = new List<Bitmap>();
        public int currentFrame = 0;

    }

    public partial class Form1 : Form
    {
        Timer tt = new Timer();
        Bitmap off;

        Bitmap bgImg;
        CActor hero = new CActor();

        bool isLoad = false;

        // We will only crop this much of the background image to "zoom in"
        int zoomWidth = 800;
        int zoomHeight = 450;
        int camX = 0; // Where the camera is currently looking

        int camY = 0;
        int maxCamX = 0;

        // JUMP PHYSICS
        int groundY;
        int verticalVelocity = 0;
        int gravity = 2;
        bool isJumping = false;

        int currentLevel = 1;
        CActor elevator = new CActor();
        bool isRidingElevator = false;


        public Form1()
        {
            this.WindowState = FormWindowState.Maximized;
            tt.Tick += Tt_Tick;
            tt.Interval = 20;
            tt.Start();
            this.Load += Form1_Load;
            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            off = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isLoad == true)
            {
                //MOVE RIGHT
                if (e.KeyCode == Keys.D && isJumping == false)
                {
                    // Animate
                    hero.currentFrame += 1;
                    if (hero.currentFrame >= hero.walkRight.Count)
                    {
                        hero.currentFrame = 0;
                    }
                    hero.img = hero.walkRight[hero.currentFrame];

                    // If hero is in the middle of the screen AND there is map left, scroll
                    if (hero.X >= this.ClientSize.Width / 2 && camX < maxCamX)
                    {
                        camX += 15;
                    }
                    else if (hero.X < this.ClientSize.Width - hero.W) // Otherwise, move hero
                    {
                        hero.X += 10;
                    }
                }

                //MOVE LEFT
                if (e.KeyCode == Keys.A && isJumping == false)
                {
                    // Animate
                    hero.currentFrame += 1;
                    if (hero.currentFrame >= hero.walkLeft.Count)
                    {
                        hero.currentFrame = 0;
                    }
                    hero.img = hero.walkLeft[hero.currentFrame];

                    // If hero is in the middle of the screen AND there is map behind him, scroll!
                    if (hero.X <= this.ClientSize.Width / 2 && camX > 0)
                    {
                        camX -= 15;
                    }
                    else if (hero.X > 0) // Otherwise, move hero
                    {
                        hero.X -= 10;
                    }
                }

                
                if (e.KeyCode == Keys.Space && isJumping == false)
                {
                    isJumping = true;
                    verticalVelocity = -30;
                }
            }
        }
        void LoadLevel()
        {
            if (currentLevel == 1)
            {
                bgImg = new Bitmap("Battleground1.png");
                elevator.W = 150;
                elevator.H = 30;
                elevator.X = bgImg.Width - 400;
            }
            else if (currentLevel == 2)
            {
                bgImg = new Bitmap("Level2_Background.png");
                elevator.X = -1000;
                isRidingElevator = false;
            }

            // We multiply first, then divide to keep the exact aspect ratio
            zoomWidth = bgImg.Width / 2;
            zoomHeight = (zoomWidth * this.ClientSize.Height) / this.ClientSize.Width;

            maxCamX = bgImg.Width - zoomWidth;
            camY = (bgImg.Height - zoomHeight) / 2;

            if (hero.walkRight.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    Bitmap r = new Bitmap("herowalk_right_" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    hero.walkRight.Add(r);

                    Bitmap l = new Bitmap("herowalk_left_" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    hero.walkLeft.Add(l);
                }
            }

            hero.img = hero.walkRight[0];
            int scale = 4;
            hero.W = hero.img.Width * scale;
            hero.H = hero.img.Height * scale;

            groundY = this.ClientSize.Height - hero.H - 120;
            hero.Y = groundY;
            elevator.Y = groundY + hero.H;

            if (currentLevel == 1)
            {
                camX = 0;
                hero.X = 100;
            }
            else if (currentLevel == 2)
            {
                camX = maxCamX;   // Spawn at the right side of the map (the gates)
                hero.X = this.ClientSize.Width / 2 + 100; // Spawn hero near the gates
            }

            isLoad = true;
        }
        private void Tt_Tick(object sender, EventArgs e)
        {
            if (isLoad == true)
            {
                if (isJumping == true)
                {
                    hero.Y += verticalVelocity;
                    verticalVelocity += gravity;

                    if (hero.Y >= groundY)
                    {
                        hero.Y = groundY;
                        isJumping = false;
                        verticalVelocity = 0;
                    }
                }

                if (currentLevel == 1)
                {
                    int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;

                    if (hero.X + hero.W >= elevatorScreenX && hero.X <= elevatorScreenX + elevator.W)
                    {
                        isRidingElevator = true;
                    }

                    if (isRidingElevator == true)
                    {
                        elevator.Y += 5;
                        hero.Y += 5;

                        hero.X = elevatorScreenX + (elevator.W / 2) - (hero.W / 2);

                        if (elevator.Y > this.ClientSize.Height)
                        {
                            currentLevel = 2;
                            LoadLevel();
                        }
                    }
                }

                DrawDubb(this.CreateGraphics());
            }
            
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (isLoad == false)
            {
                LoadLevel();
            }

            DrawDubb(e.Graphics);
        }

        void DrawDubb(Graphics g)
        {
            if (off != null)
            {
                Graphics g2 = Graphics.FromImage(off);
                DrawScene(g2);
                g.DrawImage(off, 0, 0);
            }
        }

        void DrawScene(Graphics g2)
        {
            g2.Clear(Color.Black);
            g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            // 1. Draw Background
            Rectangle rcDst = new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            Rectangle rcSrc = new Rectangle(camX, camY, zoomWidth, zoomHeight);
            g2.DrawImage(bgImg, rcDst, rcSrc, GraphicsUnit.Pixel);

            // 2. Draw Hero directly to screen coordinates
            g2.DrawImage(hero.img, hero.X, hero.Y, hero.W, hero.H);

            if (currentLevel == 1)
            {
                int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.FillRectangle(Brushes.DarkGray, elevatorScreenX, elevator.Y, elevator.W, elevator.H);
                g2.DrawRectangle(Pens.Black, elevatorScreenX, elevator.Y, elevator.W, elevator.H);
            }
        }
    }
}
