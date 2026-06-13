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

        public List<Bitmap> shootRight = new List<Bitmap>();
        public List<Bitmap> shootLeft = new List<Bitmap>();

        public List<Bitmap> fightRight = new List<Bitmap>();
        public List<Bitmap> fightLeft = new List<Bitmap>();

        public int currentFrame = 0;
        public int dx = 1;
    }

    // =======================================================
    // 2. THE MAIN ENGINE FORM
    // =======================================================
    public partial class Form1 : Form
    {
        Timer tt = new Timer();
        Bitmap off;
        bool isLoad = false;

        Bitmap bgImg;
        CActor hero = new CActor();
        CActor elevator = new CActor();
        CActor ladder = new CActor();
        CActor enemy = new CActor();
        CActor keyDrop = new CActor();

        int zoomWidth = 800;
        int zoomHeight = 450;
        int camX = 0;
        int camY = 0;
        int maxCamX = 0;

        int groundY;
        int verticalVelocity = 0;
        int gravity = 2;
        bool isJumping = false;
        bool isClimbing = false;

        int heroState = 0; // 0 = Idle/Walk, 1 = Shooting
        List<CActor> heroBullets = new List<CActor>();

        int currentLevel = 1;
        bool isRidingElevator = false;
        bool isEnemyAlive = true;
        int enemyHealth = 6;
        bool isKeyDropped = false;
        bool hasKey = false;

        int enemyMinX;
        int enemyMaxX;

        const int STATE_PATROL = 0;
        const int STATE_FIGHT = 1;
        int enemyState = STATE_PATROL;

        int heroHealth = 100;
        int heroDamageCooldown = 0;

        public Form1()
        {
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            InitializeComponent();

            this.Load += Form1_Load;
            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;

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

                for (int i = 1; i <= 5; i++)
                {
                    Bitmap r = new Bitmap("righthero_shoot_" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    hero.shootRight.Add(r);

                    Bitmap l = new Bitmap("lefthero_shoot_" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    hero.shootLeft.Add(l);
                }
            }
        }

        void InitEnemySprites()
        {
            if (enemy.walkRight.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    Bitmap r = new Bitmap("samurai_walk_R" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    enemy.walkRight.Add(r);

                    Bitmap l = new Bitmap("samurai_walk_L" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    enemy.walkLeft.Add(l);
                }

                for (int i = 1; i <= 4; i++)
                {
                    Bitmap r = new Bitmap("samurai_shoot_R" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    enemy.fightRight.Add(r);

                    Bitmap l = new Bitmap("samurai_shoot_L" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    enemy.fightLeft.Add(l);
                }
            }
        }

        // =======================================================
        // 4. HERO COMBAT & MECHANICS ENGINE
        // =======================================================

        void MoveHeroRight()
        {
            hero.dx = 1;
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
            hero.dx = -1;
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
            int currentFloorY = groundY;

            if (hero.X + (hero.W / 2) >= 0 && hero.X + (hero.W / 2) <= elevator.X + elevator.W)
            {
                if (hero.Y + hero.H <= ladder.Y + 20)
                {
                    currentFloorY = ladder.Y - hero.H;
                }
            }

            if (isClimbing == false)
            {
                if (hero.Y < currentFloorY || isJumping == true)
                {
                    hero.Y += verticalVelocity;
                    verticalVelocity += gravity;

                    if (hero.Y >= currentFloorY)
                    {
                        hero.Y = currentFloorY;
                        isJumping = false;
                        verticalVelocity = 0;
                    }
                }
            }
        }

        void CreateHeroMagic()
        {
            CActor magic = new CActor();
            magic.W = 20;
            magic.H = 20;
            magic.dx = hero.dx;

            magic.Y = hero.Y + (hero.H / 4) + 5;

            int heroMapX = ((hero.X * zoomWidth) / this.ClientSize.Width) + camX;
            int heroMapW = (hero.W * zoomWidth) / this.ClientSize.Width;
            int magicMapW = (magic.W * zoomWidth) / this.ClientSize.Width;
            int mapOffset = (15 * zoomWidth) / this.ClientSize.Width;

            if (hero.dx == 1)
            {
                magic.X = heroMapX + heroMapW - mapOffset;
            }
            else
            {
                magic.X = heroMapX - magicMapW + mapOffset;
            }

            heroBullets.Add(magic);
        }

        void UpdateHeroShooting()
        {
            if (heroState == 1)
            {
                hero.currentFrame++;

                // Cleaned up the ternary operator here
                List<Bitmap> currentList = new List<Bitmap>();
                if (hero.dx == 1)
                {
                    currentList = hero.shootRight;
                }
                else
                {
                    currentList = hero.shootLeft;
                }

                if (hero.currentFrame >= currentList.Count)
                {
                    heroState = 0;
                    hero.currentFrame = 0;

                    // Cleaned up the ternary operator here
                    if (hero.dx == 1)
                    {
                        if (hero.walkRight.Count > 0)
                        {
                            hero.img = hero.walkRight[0];
                        }
                    }
                    else
                    {
                        if (hero.walkLeft.Count > 0)
                        {
                            hero.img = hero.walkLeft[0];
                        }
                    }
                }
                else
                {
                    hero.img = currentList[hero.currentFrame];

                    if (hero.currentFrame == 3)
                    {
                        CreateHeroMagic();
                    }
                }
            }

            for (int i = heroBullets.Count - 1; i >= 0; i--)
            {
                CActor b = heroBullets[i];
                b.X += b.dx * 35;

                int bulletScreenX = ((b.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (bulletScreenX < -50 || bulletScreenX > this.ClientSize.Width + 50)
                {
                    heroBullets.RemoveAt(i);
                    continue;
                }

                if (isEnemyAlive == true)
                {
                    int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

                    if (bulletScreenX + b.W >= enemyScreenX && bulletScreenX <= enemyScreenX + enemy.W &&
                        b.Y + b.H >= enemy.Y && b.Y <= enemy.Y + enemy.H)
                    {
                        heroBullets.RemoveAt(i);
                        enemyHealth -= 1;

                        if (enemyHealth <= 0)
                        {
                            isEnemyAlive = false;
                            isKeyDropped = true;
                            keyDrop.X = enemy.X + (enemy.W / 2);
                            keyDrop.Y = enemy.Y + enemy.H - 20;
                            keyDrop.W = 30;
                            keyDrop.H = 20;
                        }
                    }
                }
            }
        }

        // =======================================================
        // 5. ENEMY BEHAVIOR ENGINE
        // =======================================================

        void UpdateEnemyAI()
        {
            if (isEnemyAlive == false)
            {
                return;
            }

            int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

            if (hero.X >= enemyScreenX - 250 && hero.X <= enemyScreenX + 250 &&
                hero.Y >= enemy.Y - 150 && hero.Y <= enemy.Y + 150)
            {
                if (enemyState != STATE_FIGHT)
                {
                    enemyState = STATE_FIGHT;
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
            else
            {
                if (enemyState != STATE_PATROL)
                {
                    enemyState = STATE_PATROL;
                }
            }

            if (enemyState == STATE_PATROL)
            {
                enemy.X += enemy.dx * 5;
                enemy.currentFrame += 1;

                if (enemy.dx == 1)
                {
                    if (enemy.currentFrame >= enemy.walkRight.Count)
                    {
                        enemy.currentFrame = 0;
                    }
                    if (enemy.walkRight.Count > 0)
                    {
                        enemy.img = enemy.walkRight[enemy.currentFrame];
                    }
                    if (enemy.X >= enemyMaxX)
                    {
                        enemy.dx = -1;
                    }
                }
                else
                {
                    if (enemy.currentFrame >= enemy.walkLeft.Count)
                    {
                        enemy.currentFrame = 0;
                    }
                    if (enemy.walkLeft.Count > 0)
                    {
                        enemy.img = enemy.walkLeft[enemy.currentFrame];
                    }
                    if (enemy.X <= enemyMinX)
                    {
                        enemy.dx = 1;
                    }
                }
            }
            else if (enemyState == STATE_FIGHT)
            {
                enemy.currentFrame += 1;

                // Cleaned up the ternary operator here
                List<Bitmap> currentFightList = new List<Bitmap>();
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
        }

        void CheckCollisions()
        {
            if (isEnemyAlive == true)
            {
                int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= enemyScreenX && hero.X <= enemyScreenX + enemy.W &&
                    hero.Y + hero.H >= enemy.Y && hero.Y <= enemy.Y + enemy.H)
                {
                    if (isJumping == true && verticalVelocity > 0)
                    {
                        enemyHealth -= 1;
                        verticalVelocity = -25;

                        if (enemyHealth <= 0)
                        {
                            isEnemyAlive = false;
                            isKeyDropped = true;
                            keyDrop.X = enemy.X + (enemy.W / 2);
                            keyDrop.Y = enemy.Y + enemy.H - 20;
                            keyDrop.W = 30;
                            keyDrop.H = 20;
                        }
                    }
                    else if (heroDamageCooldown == 0 && enemyState == STATE_FIGHT)
                    {
                        heroHealth -= 25;
                        heroDamageCooldown = 40;

                        verticalVelocity = -15;
                        isJumping = true;

                        if (hero.X < enemyScreenX)
                        {
                            hero.X -= 50;
                        }
                        else
                        {
                            hero.X += 50;
                        }

                        if (heroHealth <= 0)
                        {
                            LoadLevel();
                            return;
                        }
                    }
                }
            }

            if (isKeyDropped == true && hasKey == false)
            {
                int keyScreenX = ((keyDrop.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= keyScreenX && hero.X <= keyScreenX + keyDrop.W &&
                    hero.Y + hero.H >= keyDrop.Y && hero.Y <= keyDrop.Y + keyDrop.H)
                {
                    hasKey = true;
                    isKeyDropped = false;
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

            enemyHealth = 6;
            isEnemyAlive = true;
            hasKey = false;
            isKeyDropped = false;
            heroBullets.Clear();
            heroHealth = 100;
            heroDamageCooldown = 0;

            if (currentLevel == 1)
            {
                if (enemy.walkLeft.Count > 0)
                {
                    enemy.img = enemy.walkLeft[0];
                }

                if (enemy.img != null)
                {
                    enemy.W = enemy.img.Width * 4;
                    enemy.H = enemy.img.Height * 4;
                }

                enemyMinX = 0;
                enemyMaxX = elevator.X - 150;

                Random RR = new Random();
                enemy.X = RR.Next(enemyMinX, enemyMaxX);
                enemy.Y = ladder.Y - enemy.H;

                camX = 0;
                hero.X = 100;
            }
            else if (currentLevel == 2)
            {
                isEnemyAlive = false;

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
                    if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
                    {
                        isClimbing = true;
                        isJumping = false;
                        verticalVelocity = 0;
                        hero.Y -= 15;
                        hero.X = ladderScreenX + (ladder.W / 2) - (hero.W / 2);

                        int topFloorY = ladder.Y - hero.H;
                        if (hero.Y <= topFloorY)
                        {
                            hero.Y = topFloorY;
                            isClimbing = false;
                        }
                        return;
                    }
                    if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down)
                    {
                        isClimbing = true;
                        isJumping = false;
                        verticalVelocity = 0;
                        hero.Y += 15;
                        hero.X = ladderScreenX + (ladder.W / 2) - (hero.W / 2);

                        if (hero.Y >= groundY)
                        {
                            hero.Y = groundY;
                            isClimbing = false;
                        }
                        return;
                    }

                    if (isClimbing == true && (e.KeyCode == Keys.A || e.KeyCode == Keys.D || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
                    {
                        isClimbing = false;
                    }
                }

                if (e.KeyCode == Keys.F && isClimbing == false && heroState == 0)
                {
                    heroState = 1;
                    hero.currentFrame = 0;
                }

                if (isClimbing == false && heroState == 0)
                {
                    if ((e.KeyCode == Keys.D || e.KeyCode == Keys.Right) && isJumping == false)
                    {
                        MoveHeroRight();
                    }

                    if ((e.KeyCode == Keys.A || e.KeyCode == Keys.Left) && isJumping == false)
                    {
                        MoveHeroLeft();
                    }

                    if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Up) && isJumping == false)
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
                if (heroDamageCooldown > 0)
                {
                    heroDamageCooldown--;
                }

                UpdateHeroPhysics();
                UpdateHeroShooting();
                HandleElevatorMechanics();
                UpdateEnemyAI();
                CheckCollisions();

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
                int platformStartScreenX = ((0 - camX) * this.ClientSize.Width) / zoomWidth;
                int platformEndScreenX = ((elevator.X + elevator.W - camX) * this.ClientSize.Width) / zoomWidth;
                int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;

                g2.FillRectangle(Brushes.DarkSlateGray, platformStartScreenX, ladder.Y, platformEndScreenX - platformStartScreenX, 20);
                g2.DrawRectangle(Pens.Black, platformStartScreenX, ladder.Y, platformEndScreenX - platformStartScreenX, 20);

                g2.FillRectangle(Brushes.DarkGray, elevatorScreenX, elevator.Y, elevator.W, elevator.H);
                g2.DrawRectangle(Pens.Black, elevatorScreenX, elevator.Y, elevator.W, elevator.H);

                Pen ladderPen = new Pen(Color.SaddleBrown, 6);
                g2.DrawLine(ladderPen, ladderScreenX + 10, ladder.Y, ladderScreenX + 10, ladder.Y + ladder.H);
                g2.DrawLine(ladderPen, ladderScreenX + ladder.W - 10, ladder.Y, ladderScreenX + ladder.W - 10, ladder.Y + ladder.H);

                for (int y = ladder.Y + 20; y < ladder.Y + ladder.H; y += 30)
                {
                    g2.DrawLine(ladderPen, ladderScreenX + 10, y, ladderScreenX + ladder.W - 10, y);
                }
            }

            if (isKeyDropped == true)
            {
                int keyScreenX = ((keyDrop.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.FillRectangle(Brushes.Gold, keyScreenX, keyDrop.Y, keyDrop.W, keyDrop.H);
            }

            if (currentLevel == 1 && isEnemyAlive == true && enemy.img != null)
            {
                int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.DrawImage(enemy.img, enemyScreenX, enemy.Y, enemy.W, enemy.H);

                g2.FillRectangle(Brushes.Red, enemyScreenX, enemy.Y + 20, (enemy.W / 6) * enemyHealth, 10);
            }

            // 5. DRAW HERO
            if (hero.img != null)
            {
                if (heroDamageCooldown == 0 || heroDamageCooldown % 4 < 2)
                {
                    g2.DrawImage(hero.img, hero.X, hero.Y, hero.W, hero.H);
                }
            }

            // 6. DRAW HUD ALERTS & HEALTH BARS
            g2.DrawString("HERO HP", new Font("Arial", 12, FontStyle.Bold), Brushes.White, 20, 10);
            g2.FillRectangle(Brushes.Gray, 20, 30, 200, 20);
            g2.FillRectangle(Brushes.LimeGreen, 20, 30, heroHealth * 2, 20);
            g2.DrawRectangle(Pens.White, 20, 30, 200, 20);

            for (int i = 0; i < heroBullets.Count; i++)
            {
                CActor b = heroBullets[i];
                int bulletScreenX = ((b.X - camX) * this.ClientSize.Width) / zoomWidth;

                g2.FillEllipse(Brushes.Cyan, bulletScreenX, b.Y, b.W, b.H);
                g2.FillEllipse(Brushes.White, bulletScreenX + 4, b.Y + 4, b.W - 8, b.H - 8);
            }

            if (hasKey == true)
            {
                g2.FillRectangle(Brushes.Gold, 50, 60, 30, 20);
                g2.DrawString("Key Acquired", new Font("Arial", 12), Brushes.White, 90, 60);
            }
        }
    }
}