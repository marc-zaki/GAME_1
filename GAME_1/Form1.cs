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
    // =======================================================
    // 1. THE ACTOR BLUEPRINT CLASS
    // =======================================================
    public class CActor
    {
        public int X, Y;
        public int W, H;
        public Bitmap img;
        public List<Bitmap> walkRight = new List<Bitmap>();
        public List<Bitmap> walkLeft = new List<Bitmap>();

        // Directional up-close fight frames
        public List<Bitmap> fightRight = new List<Bitmap>();
        public List<Bitmap> fightLeft = new List<Bitmap>();

        // Directional ranged sword-wave frames
        public List<Bitmap> shootRight = new List<Bitmap>();
        public List<Bitmap> shootLeft = new List<Bitmap>();

        public int currentFrame = 0;
        public int dx = 1;
    }

    // =======================================================
    // 2. THE MAIN ENGINE FORM
    // =======================================================
    public partial class Form1 : Form
    {
        // Engine Core Properties
        Timer tt = new Timer();
        Bitmap off;
        bool isLoad = false;

        // Environment Entities
        Bitmap bgImg;
        CActor hero = new CActor();
        CActor elevator = new CActor();
        CActor ladder = new CActor();
        CActor enemy = new CActor();
        List<CActor> LActsEnemyBullets = new List<CActor>();

        // Camera Tracking Engine Coordinates
        int zoomWidth = 800;
        int zoomHeight = 450;
        int camX = 0;
        int camY = 0;
        int maxCamX = 0;

        // Pure Integer Jump Physics & State
        int groundY;
        int verticalVelocity = 0;
        int gravity = 2;
        bool isJumping = false;
        bool isClimbing = false;

        // Level & Puzzle Flow Management Flags
        int currentLevel = 1;
        bool isRidingElevator = false;
        bool isEnemyAlive = true;
        bool hasKey = false;

        // Enemy Patrolling Bounds (Map Coordinates)
        int enemyMinX;
        int enemyMaxX;

        // Enemy State Machine Constants
        const int STATE_PATROL = 0;
        const int STATE_FIGHT = 1;
        const int STATE_SHOOT = 2;
        int enemyState = STATE_PATROL;

        public Form1()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();

            // Wire up structural layout events
            this.Load += Form1_Load;
            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;

            // Core game loop configuration
            tt.Tick += Tt_Tick;
            tt.Interval = 20;
            tt.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            off = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
        }

        // =======================================================
        // 3. ASSET INITIALIZATION FUNCTIONS
        // =======================================================

        void InitHeroSprites()
        {
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
        }

        void CreateSwordWave()
        {
            CActor wave = new CActor();
            wave.img = new Bitmap("sword_wave.png");
            wave.img.MakeTransparent(wave.img.GetPixel(0, 0));
            wave.W = 20;
            wave.H = 50;

            wave.Y = enemy.Y + (enemy.H / 2) - (wave.H / 2);
            wave.dx = enemy.dx;

            if (enemy.dx == 1)
            {
                wave.X = enemy.X + enemy.W;
            }
            else
            {
                wave.X = enemy.X - wave.W;
            }

            LActsEnemyBullets.Add(wave);
        }

        void InitEnemySprites()
        {
            if (enemy.walkRight.Count == 0)
            {
                // 1. Load Enemy Walking Frames (4 frames)
                for (int i = 1; i <= 4; i++)
                {
                    Bitmap r = new Bitmap("samurai_walk_R" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    enemy.walkRight.Add(r);

                    Bitmap l = new Bitmap("samurai_walk_L" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    enemy.walkLeft.Add(l);
                }

                // 2. Load Ranged Sword Wave Attack Frames (4 frames)
                for (int i = 1; i <= 4; i++)
                {
                    Bitmap r = new Bitmap("samurai_shoot_R" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    enemy.shootRight.Add(r);

                    Bitmap l = new Bitmap("samurai_shoot_L" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    enemy.shootLeft.Add(l);
                }

                // 3. Load Close Combat Melee Fight Frames Verbatim
                for (int i = 1; i <= 4; i++)
                {
                    string rPath = "";
                    string lPath = "";

                    // Handles the direct naming differences between frames 1-3 and frame 4
                    if (i <= 3)
                    {
                        rPath = "samurai_shoot_R" + i + ".png";
                        lPath = "samurai_shoot_L" + i + ".png";
                    }
                    else
                    {
                        rPath = "samurai_shoot_R4.png";
                        lPath = "samurai_shoot_L4.png";
                    }

                    Bitmap r = new Bitmap(rPath);
                    r.MakeTransparent(r.GetPixel(0, 0));
                    enemy.fightRight.Add(r);

                    Bitmap l = new Bitmap(lPath);
                    l.MakeTransparent(l.GetPixel(0, 0));
                    enemy.fightLeft.Add(l);
                }
            }
        }

        void UpdateEnemyAI()
        {
            if (isEnemyAlive == false) return;

            int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

            // =======================================================
            // 1. STATE DETERMINATION (LECTURE-SAFE RANGE CHECK)
            // =======================================================
            if (hero.X >= enemyScreenX - 600 && hero.X <= enemyScreenX + 600)
            {
                // If we are already fighting up close, don't break out to shoot waves
                if (enemyState != STATE_FIGHT)
                {
                    if (enemyState != STATE_SHOOT)
                    {
                        enemyState = STATE_SHOOT;
                        enemy.currentFrame = 0;
                    }

                    if (hero.X < enemyScreenX)
                    {
                        enemy.dx = -1;
                    }
                    else
                    {
                        enemy.dx = 1;
                    }
                }
            }
            else
            {
                if (enemyState != STATE_FIGHT)
                {
                    enemyState = STATE_PATROL;
                }
            }

            // Keep facing player if a close quarters combat fight is forced
            if (enemyState == STATE_FIGHT)
            {
                if (hero.X < enemyScreenX)
                {
                    enemy.dx = -1;
                }
                else
                {
                    enemy.dx = 1;
                }
            }

            // =======================================================
            // 2. STATE EXECUTION MATRIX
            // =======================================================

            // STATE 0: PATROLLING (Runs when player is far away)
            if (enemyState == STATE_PATROL)
            {
                enemy.X += enemy.dx * 5;
                enemy.currentFrame += 1;

                if (enemy.dx == 1)
                {
                    if (enemy.currentFrame >= enemy.walkRight.Count)
                        enemy.currentFrame = 0;
                    if (enemy.walkRight.Count > 0)
                        enemy.img = enemy.walkRight[enemy.currentFrame];
                    if (enemy.X >= enemyMaxX)
                        enemy.dx = -1;
                }
                else
                {
                    if (enemy.currentFrame >= enemy.walkLeft.Count)
                        enemy.currentFrame = 0;
                    if (enemy.walkLeft.Count > 0)
                        enemy.img = enemy.walkLeft[enemy.currentFrame];
                    if (enemy.X <= enemyMinX)
                        enemy.dx = 1;
                }
            }
            // STATE 1: CLOSE COMBAT MELEE (Uses your new directional fight assets)
            else if (enemyState == STATE_FIGHT)
            {
                enemy.currentFrame += 1;

                List<Bitmap> currentFightList;
                if (enemy.dx == 1)
                {
                    currentFightList = enemy.fightRight;
                }
                else
                {
                    currentFightList = enemy.fightLeft;
                }

                if (enemy.currentFrame >= currentFightList.Count)
                {
                    enemy.currentFrame = 0;
                }

                if (currentFightList.Count > 0)
                {
                    enemy.img = currentFightList[enemy.currentFrame];
                }
            }
            // STATE 2: RANGED SWORD SLASH
            else if (enemyState == STATE_SHOOT)
            {
                enemy.currentFrame += 1;

                List<Bitmap> currentShootList;
                if (enemy.dx == 1)
                {
                    currentShootList = enemy.shootRight;
                }
                else
                {
                    currentShootList = enemy.shootLeft;
                }

                if (enemy.currentFrame >= currentShootList.Count)
                {
                    enemy.currentFrame = 0;
                }

                // Release the wave projectile exactly on Frame 3 (Index 2)
                if (enemy.currentFrame == 2)
                {
                    CreateSwordWave();
                }

                if (currentShootList.Count > 0)
                {
                    enemy.img = currentShootList[enemy.currentFrame];
                }
            }

            // =======================================================
            // 3. PROJECTILE PROCESSING
            // =======================================================
            for (int i = LActsEnemyBullets.Count - 1; i >= 0; i--)
            {
                CActor b = LActsEnemyBullets[i];
                b.X += b.dx * 18;

                int bulletScreenX = ((b.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (bulletScreenX < -50 || bulletScreenX > this.ClientSize.Width + 50)
                {
                    LActsEnemyBullets.RemoveAt(i);
                }
            }
        }

        // =======================================================
        // 4. HERO CORE MECHANICS FUNCTIONS
        // =======================================================

        void MoveHeroRight()
        {
            hero.currentFrame += 1;
            if (hero.currentFrame >= hero.walkRight.Count)
            {
                hero.currentFrame = 0;
            }
            hero.img = hero.walkRight[hero.currentFrame];

            if (hero.X >= this.ClientSize.Width / 2 && camX < maxCamX)
            {
                camX += 30;
            }
            else if (hero.X < this.ClientSize.Width - hero.W)
            {
                hero.X += 25;
            }
        }

        void MoveHeroLeft()
        {
            hero.currentFrame += 1;
            if (hero.currentFrame >= hero.walkLeft.Count)
            {
                hero.currentFrame = 0;
            }
            hero.img = hero.walkLeft[hero.currentFrame];

            if (hero.X <= this.ClientSize.Width / 2 && camX > 0)
            {
                camX -= 30;
            }
            else if (hero.X > 0)
            {
                hero.X -= 25;
            }
        }

        void UpdateHeroPhysics()
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
        }

        // =======================================================
        // 5. ENEMY BEHAVIOR ENGINE FUNCTIONS
        // =======================================================

        void CheckEnemyCollision()
        {
            if (isEnemyAlive == false) return;

            int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

            if (hero.X + hero.W >= enemyScreenX && hero.X <= enemyScreenX + enemy.W &&
                hero.Y + hero.H >= enemy.Y && hero.Y <= enemy.Y + enemy.H)
            {
                if (isJumping == true && verticalVelocity > 0)
                {
                    isEnemyAlive = false;
                    hasKey = true;
                    verticalVelocity = -15;
                }
            }
        }

        // =======================================================
        // 6. MAP LOGIC & WORLD SCENE CONFIGURATION
        // =======================================================

        void HandleElevatorMechanics()
        {
            if (currentLevel == 1)
            {
                int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= elevatorScreenX && hero.X <= elevatorScreenX + elevator.W)
                {
                    if (hasKey == true)
                    {
                        isRidingElevator = true;
                    }
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
        }

        void LoadLevel()
        {
            if (currentLevel == 1)
            {
                bgImg = new Bitmap("Battleground1.png");

                elevator.W = 150;
                elevator.H = 30;
                elevator.X = bgImg.Width - 400;

                ladder.W = 80;
                ladder.H = 300;
                ladder.X = bgImg.Width / 3;
            }
            else if (currentLevel == 2)
            {
                bgImg = new Bitmap("Level2_Background.png");
                elevator.X = -1000;
                isRidingElevator = false;
            }

            zoomWidth = bgImg.Width / 2;
            zoomHeight = (zoomWidth * this.ClientSize.Height) / this.ClientSize.Width;

            maxCamX = bgImg.Width - zoomWidth;
            camY = (bgImg.Height - zoomHeight) / 2;

            InitHeroSprites();
            InitEnemySprites();

            hero.img = hero.walkRight[0];
            int scale = 4;
            hero.W = hero.img.Width * scale;
            hero.H = hero.img.Height * scale;

            groundY = this.ClientSize.Height - hero.H - 120;
            hero.Y = groundY;
            elevator.Y = groundY + hero.H;
            ladder.Y = groundY - ladder.H + hero.H;

            // Large boss proportions
            enemy.W = 120;
            enemy.H = 120;
            enemy.X = ladder.X + 150;
            enemy.Y = ladder.Y - enemy.H;
            if (enemy.walkLeft.Count > 0) enemy.img = enemy.walkLeft[0];

            enemyMinX = ladder.X + 80;
            enemyMaxX = elevator.X - 150;

            if (currentLevel == 1)
            {
                camX = 0;
                hero.X = 100;
            }
            else if (currentLevel == 2)
            {
                camX = maxCamX;
                hero.X = this.ClientSize.Width / 2 + 100;
            }

            isLoad = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isLoad == true)
            {
                int ladderScreenX = ((ladder.X - camX) * this.ClientSize.Width) / zoomWidth;
                if (hero.X + hero.W >= ladderScreenX && hero.X <= ladderScreenX + ladder.W)
                {
                    if (e.KeyCode == Keys.W)
                    {
                        isClimbing = true;
                        isJumping = false;
                        hero.Y -= 15;
                        hero.X = ladderScreenX + (ladder.W / 2) - (hero.W / 2);
                        return;
                    }
                }

                if (isClimbing == true && (e.KeyCode == Keys.A || e.KeyCode == Keys.D))
                {
                    isClimbing = false;
                }

                if (isClimbing == false)
                {
                    if (e.KeyCode == Keys.D && isJumping == false)
                    {
                        MoveHeroRight();
                    }

                    if (e.KeyCode == Keys.A && isJumping == false)
                    {
                        MoveHeroLeft();
                    }

                    if (e.KeyCode == Keys.Space && isJumping == false)
                    {
                        isJumping = true;
                        verticalVelocity = -30;
                    }
                }
            }
        }

        private void Tt_Tick(object sender, EventArgs e)
        {
            if (isLoad == true)
            {
                UpdateHeroPhysics();
                HandleElevatorMechanics();

                UpdateEnemyAI();
                CheckEnemyCollision();

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

            Rectangle rcDst = new Rectangle(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            Rectangle rcSrc = new Rectangle(camX, camY, zoomWidth, zoomHeight);

            if (bgImg != null)
            {
                g2.DrawImage(bgImg, rcDst, rcSrc, GraphicsUnit.Pixel);
            }

            if (currentLevel == 1)
            {
                int ladderScreenX = ((ladder.X - camX) * this.ClientSize.Width) / zoomWidth;
                int platformStartScreenX = ((enemyMinX - camX) * this.ClientSize.Width) / zoomWidth;
                int platformEndScreenX = ((enemyMaxX + enemy.W - camX) * this.ClientSize.Width) / zoomWidth;

                Pen platformPen = new Pen(Color.Gray, 6);
                g2.DrawLine(platformPen, platformStartScreenX, ladder.Y, platformEndScreenX, ladder.Y);

                Pen ladderPen = new Pen(Color.Brown, 4);

                g2.DrawLine(ladderPen, ladderScreenX, ladder.Y, ladderScreenX, ladder.Y + ladder.H);
                g2.DrawLine(ladderPen, ladderScreenX + ladder.W, ladder.Y, ladderScreenX + ladder.W, ladder.Y + ladder.H);

                for (int y = ladder.Y; y <= ladder.Y + ladder.H; y += 30)
                {
                    g2.DrawLine(ladderPen, ladderScreenX, y, ladderScreenX + ladder.W, y);
                }
            }

            if (currentLevel == 1 && isEnemyAlive == true && enemy.img != null)
            {
                int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.DrawImage(enemy.img, enemyScreenX, enemy.Y, enemy.W, enemy.H);
            }

            if (hero.img != null)
            {
                g2.DrawImage(hero.img, hero.X, hero.Y, hero.W, hero.H);
            }

            if (currentLevel == 1)
            {
                int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.FillRectangle(Brushes.DarkGray, elevatorScreenX, elevator.Y, elevator.W, elevator.H);
                g2.DrawRectangle(Pens.Black, elevatorScreenX, elevator.Y, elevator.W, elevator.H);
            }

            if (hasKey == true)
            {
                g2.FillRectangle(Brushes.Gold, 50, 50, 30, 20);
            }

            for (int i = 0; i < LActsEnemyBullets.Count; i++)
            {
                CActor b = LActsEnemyBullets[i];
                if (b.img != null)
                {
                    int bulletScreenX = ((b.X - camX) * this.ClientSize.Width) / zoomWidth;
                    g2.DrawImage(b.img, bulletScreenX, b.Y, b.W, b.H);
                }
            }
        }
    }
}