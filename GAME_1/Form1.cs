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

        public int health = 3;

        // Custom Bullet Logic
        public bool isMulti = false;
        public int waveId = -1;
        public List<int> hitWaves = new List<int>();
    }

    // =======================================================
    // 2. THE MAIN ENGINE FORM
    // =======================================================
    public partial class Form1 : Form
    {
        Timer tt = new Timer();
        Bitmap off;

        // GAME STATE
        bool isLoad = false;
        bool isIntro = true;
        bool isWin = false;
        Bitmap introBg;
        Bitmap bgImg;
        Bitmap bossImg;

        CActor hero = new CActor();
        CActor elevator = new CActor();
        CActor ladder = new CActor();
        CActor keyDrop = new CActor();

        // Level 1 Enemy
        CActor enemy = new CActor();
        bool isEnemyAlive = true;
        int enemyMinX;
        int enemyMaxX;
        List<CActor> enemyBullets = new List<CActor>();
        List<Bitmap> enemyBulletFrames = new List<Bitmap>();
        int enemyShootCooldown = 0;

        // Level 2 Entities 
        List<Bitmap> portalFrames = new List<Bitmap>();
        CActor portal = new CActor();
        int portalTimer = 0;
        int portalAnimTick = 0;

        List<CActor> goblins = new List<CActor>();
        List<Bitmap> goblinWalkR = new List<Bitmap>();
        List<Bitmap> goblinWalkL = new List<Bitmap>();

        CActor movingPlatform = new CActor();
        int platformDir = -1;
        bool isPlatformMoving = false;
        int pitStart = 0;
        int pitEnd = 0;

        // Level 3 Boss
        bool isFiringLaser = false;
        List<CActor> turrets = new List<CActor>();

        // Bullet Logic
        List<CActor> heroBullets = new List<CActor>();
        bool isMultipleShooting = false;
        int waveCounter = 0;

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

        int heroState = 0;

        int currentLevel = 1;
        bool isRidingElevator = false;
        bool isKeyDropped = false;
        bool hasKey = false;

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
            this.MouseClick += Form1_MouseClick;

            tt.Tick += Tt_Tick;
            tt.Interval = 20;
            tt.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            off = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            try { introBg = new Bitmap("intro_bg.png"); } catch { }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (isIntro)
            {
                Rectangle playBtn = new Rectangle(this.ClientSize.Width / 2 - 100, this.ClientSize.Height / 2, 200, 60);
                if (playBtn.Contains(e.Location))
                {
                    isIntro = false;
                    LoadLevel();
                }
            }
        }

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

            if (goblinWalkR.Count == 0)
            {
                for (int i = 1; i <= 3; i++)
                {
                    Bitmap r = new Bitmap("goblin_right" + i + ".png");
                    r.MakeTransparent(r.GetPixel(0, 0));
                    goblinWalkR.Add(r);

                    Bitmap l = new Bitmap("goblin_left" + i + ".png");
                    l.MakeTransparent(l.GetPixel(0, 0));
                    goblinWalkL.Add(l);
                }
            }

            if (enemyBulletFrames.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    try
                    {
                        Bitmap b = new Bitmap("bull_e" + i + ".png");
                        b.MakeTransparent(b.GetPixel(0, 0));
                        enemyBulletFrames.Add(b);
                    }
                    catch { }
                }
            }
        }

        void InitPortalSprites()
        {
            if (portalFrames.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    try
                    {
                        Bitmap p = new Bitmap("portal" + i + ".png");
                        portalFrames.Add(p);
                    }
                    catch { }
                }
            }
        }

        void MoveHeroRight()
        {
            hero.dx = 1;
            hero.currentFrame += 1;

            if (hero.currentFrame >= hero.walkRight.Count) hero.currentFrame = 0;
            if (hero.walkRight.Count > 0) hero.img = hero.walkRight[hero.currentFrame];

            if (hero.X >= this.ClientSize.Width / 2 && camX < maxCamX)
            {
                camX += 30;
                if (camX > maxCamX)
                {
                    int remainder = camX - maxCamX;
                    camX = maxCamX;
                    hero.X += (remainder * this.ClientSize.Width) / zoomWidth;
                }
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

            if (hero.currentFrame >= hero.walkLeft.Count) hero.currentFrame = 0;
            if (hero.walkLeft.Count > 0) hero.img = hero.walkLeft[hero.currentFrame];

            if (hero.X <= this.ClientSize.Width / 2 && camX > 0)
            {
                camX -= 30;
                if (camX < 0)
                {
                    int remainder = 0 - camX;
                    camX = 0;
                    hero.X -= (remainder * this.ClientSize.Width) / zoomWidth;
                }
            }
            else if (hero.X > 0)
            {
                hero.X -= 25;
            }
        }

        void UpdateHeroPhysics()
        {
            int currentFloorY = groundY;

            if (hero.X + (hero.W / 2) >= 0 && hero.X + (hero.W / 2) <= elevator.X + elevator.W && currentLevel == 1)
            {
                if (hero.Y + hero.H <= ladder.Y + 20)
                {
                    currentFloorY = ladder.Y - hero.H;
                }
            }

            if (currentLevel == 2)
            {
                int heroWorldX = ((hero.X * zoomWidth) / this.ClientSize.Width) + camX;

                if (heroWorldX > pitStart && heroWorldX < pitEnd)
                {
                    currentFloorY = this.ClientSize.Height + 500;
                }
            }

            if (currentLevel == 3)
            {
                currentFloorY = bgImg.Height - hero.H - 50;
            }

            if (currentLevel == 2)
            {
                int platformScreenX = ((movingPlatform.X - camX) * this.ClientSize.Width) / zoomWidth;
                int platformWorldSpeed = 15;
                int screenSpeed = (platformWorldSpeed * this.ClientSize.Width) / zoomWidth;

                if (hero.X + (hero.W / 2) >= platformScreenX && hero.X + (hero.W / 2) <= platformScreenX + movingPlatform.W)
                {
                    if (hero.Y + hero.H <= movingPlatform.Y + 20)
                    {
                        currentFloorY = movingPlatform.Y - hero.H;
                        isPlatformMoving = true;

                        if (isPlatformMoving && !isJumping && isClimbing == false)
                        {
                            if (platformDir == 1)
                            {
                                if (hero.X >= this.ClientSize.Width / 2 && camX < maxCamX)
                                {
                                    camX += platformWorldSpeed;
                                    if (camX > maxCamX)
                                    {
                                        int remainder = camX - maxCamX;
                                        camX = maxCamX;
                                        hero.X += (remainder * this.ClientSize.Width) / zoomWidth;
                                    }
                                }
                                else hero.X += screenSpeed;
                            }
                            else
                            {
                                if (hero.X <= this.ClientSize.Width / 2 && camX > 0)
                                {
                                    camX -= platformWorldSpeed;
                                    if (camX < 0)
                                    {
                                        int remainder = 0 - camX;
                                        camX = 0;
                                        hero.X -= (remainder * this.ClientSize.Width) / zoomWidth;
                                    }
                                }
                                else hero.X -= screenSpeed;
                            }
                        }
                    }
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
            int numberOfBullets = isMultipleShooting ? 3 : 1;
            int baseShootY = hero.Y + (hero.H / 2) + 40;

            if (isMultipleShooting) waveCounter++;

            for (int i = 0; i < numberOfBullets; i++)
            {
                CActor magic = new CActor();
                magic.W = 20;
                magic.H = 20;
                magic.dx = hero.dx;
                magic.isMulti = isMultipleShooting;
                magic.waveId = isMultipleShooting ? waveCounter : -1;

                if (isMultipleShooting)
                {
                    int spreadDistance = 40;
                    magic.Y = baseShootY + (i * spreadDistance) - spreadDistance;
                }
                else
                {
                    magic.Y = baseShootY;
                }

                int heroMapX = ((hero.X * zoomWidth) / this.ClientSize.Width) + camX;
                int heroMapW = (hero.W * zoomWidth) / this.ClientSize.Width;
                int magicMapW = (magic.W * zoomWidth) / this.ClientSize.Width;
                int mapOffset = (15 * zoomWidth) / this.ClientSize.Width;

                if (hero.dx == 1) magic.X = heroMapX + heroMapW - mapOffset;
                else magic.X = heroMapX - magicMapW + mapOffset;

                heroBullets.Add(magic);
            }
        }

        void UpdateHeroShooting()
        {
            if (heroState == 1)
            {
                hero.currentFrame++;

                List<Bitmap> currentList = new List<Bitmap>();
                if (hero.dx == 1) currentList = hero.shootRight;
                else currentList = hero.shootLeft;

                if (hero.currentFrame >= currentList.Count)
                {
                    heroState = 0;
                    hero.currentFrame = 0;

                    if (hero.dx == 1)
                    {
                        if (hero.walkRight.Count > 0) hero.img = hero.walkRight[0];
                    }
                    else
                    {
                        if (hero.walkLeft.Count > 0) hero.img = hero.walkLeft[0];
                    }
                }
                else
                {
                    hero.img = currentList[hero.currentFrame];
                    if (hero.currentFrame == 3) CreateHeroMagic();
                }
            }

            if (isFiringLaser && currentLevel == 3)
            {
                for (int i = turrets.Count - 1; i >= 0; i--)
                {
                    CActor t = turrets[i];
                    int tScreenX = ((t.X - camX) * this.ClientSize.Width) / zoomWidth;
                    bool hit = false;

                    if (hero.dx == 1 && tScreenX > hero.X && (t.Y + t.H > hero.Y && t.Y < hero.Y + hero.H)) hit = true;
                    if (hero.dx == -1 && tScreenX < hero.X && (t.Y + t.H > hero.Y && t.Y < hero.Y + hero.H)) hit = true;

                    if (hit)
                    {
                        t.health -= 1;
                        if (t.health <= 0) turrets.RemoveAt(i);
                    }
                }
            }

            for (int i = heroBullets.Count - 1; i >= 0; i--)
            {
                CActor b = heroBullets[i];
                b.X += b.dx * 35;

                int bulletScreenX = ((b.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (bulletScreenX < -1000 || bulletScreenX > this.ClientSize.Width + 1000)
                {
                    heroBullets.RemoveAt(i);
                    continue;
                }

                if (currentLevel == 1 && isEnemyAlive == true)
                {
                    int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

                    if (bulletScreenX + b.W >= enemyScreenX && bulletScreenX <= enemyScreenX + enemy.W &&
                        b.Y + b.H >= enemy.Y && b.Y <= enemy.Y + enemy.H)
                    {
                        heroBullets.RemoveAt(i);
                        enemy.health -= 1;

                        if (enemy.health <= 0) DropKey(enemy);
                        continue;
                    }
                }

                if (currentLevel == 2)
                {
                    bool destroyed = false;
                    for (int j = goblins.Count - 1; j >= 0; j--)
                    {
                        CActor gob = goblins[j];
                        int gobScreenX = ((gob.X - camX) * this.ClientSize.Width) / zoomWidth;

                        if (bulletScreenX + b.W >= gobScreenX && bulletScreenX <= gobScreenX + gob.W &&
                            b.Y + b.H >= gob.Y && b.Y <= gob.Y + gob.H)
                        {
                            heroBullets.RemoveAt(i);
                            gob.health -= 1;

                            if (gob.health <= 0)
                            {
                                if (goblins.Count == 1) DropKey(gob);
                                goblins.RemoveAt(j);
                            }
                            destroyed = true;
                            break;
                        }
                    }
                    if (destroyed) continue;
                }

                if (currentLevel == 3)
                {
                    bool destroyed = false;
                    for (int j = turrets.Count - 1; j >= 0; j--)
                    {
                        CActor t = turrets[j];
                        int tScreenX = ((t.X - camX) * this.ClientSize.Width) / zoomWidth;

                        if (bulletScreenX + b.W >= tScreenX && bulletScreenX <= tScreenX + t.W &&
                            b.Y + b.H >= t.Y && b.Y <= t.Y + t.H)
                        {
                            if (b.isMulti)
                            {
                                if (!t.hitWaves.Contains(b.waveId))
                                {
                                    t.hitWaves.Add(b.waveId);
                                    t.health -= 1;
                                    if (t.health <= 0)
                                    {
                                        turrets.RemoveAt(j);
                                        if (turrets.Count == 0) isWin = true;
                                    }
                                }
                            }

                            heroBullets.RemoveAt(i);
                            destroyed = true;
                            break;
                        }
                    }
                    if (destroyed) continue;
                }
            }

            // Enemy bullet movement and hero collision (Level 1)
            if (currentLevel == 1)
            {
                for (int i = enemyBullets.Count - 1; i >= 0; i--)
                {
                    CActor eb = enemyBullets[i];
                    eb.X += eb.dx * 20;

                    // Animate through frames
                    if (enemyBulletFrames.Count > 0)
                    {
                        eb.currentFrame = (eb.currentFrame + 1) % enemyBulletFrames.Count;
                        eb.img = enemyBulletFrames[eb.currentFrame];
                    }

                    int ebScreenX = ((eb.X - camX) * this.ClientSize.Width) / zoomWidth;

                    // Remove if off screen
                    if (ebScreenX < -200 || ebScreenX > this.ClientSize.Width + 200)
                    {
                        enemyBullets.RemoveAt(i);
                        continue;
                    }

                    // Hit hero
                    if (ebScreenX + eb.W >= hero.X && ebScreenX <= hero.X + hero.W &&
                        eb.Y + eb.H >= hero.Y && eb.Y <= hero.Y + hero.H)
                    {
                        enemyBullets.RemoveAt(i);
                        if (heroDamageCooldown == 0)
                        {
                            heroHealth -= 20;
                            heroDamageCooldown = 40;
                            if (heroHealth <= 0)
                            {
                                heroHealth = 0;
                                LoadLevel();
                                return;
                            }
                        }
                    }
                }
            }
        }

        void DropKey(CActor deadEnemy)
        {
            if (currentLevel == 1) isEnemyAlive = false;

            isKeyDropped = true;
            keyDrop.X = deadEnemy.X + (deadEnemy.W / 2);
            keyDrop.Y = deadEnemy.Y + deadEnemy.H - 20;
            keyDrop.W = 30;
            keyDrop.H = 20;
        }

        void UpdateEnemyAI()
        {
            if (currentLevel == 1 && isEnemyAlive)
            {
                int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X >= enemyScreenX - 250 && hero.X <= enemyScreenX + 250 &&
                    hero.Y >= enemy.Y - 150 && hero.Y <= enemy.Y + 150)
                {
                    if (enemyState != STATE_FIGHT)
                    {
                        enemyState = STATE_FIGHT;
                        enemy.currentFrame = 0;
                    }
                    if (hero.X < enemyScreenX) enemy.dx = -1;
                    else enemy.dx = 1;
                }
                else
                {
                    if (enemyState != STATE_PATROL) enemyState = STATE_PATROL;
                }

                if (enemyState == STATE_PATROL)
                {
                    enemy.X += enemy.dx * 5;
                    enemy.currentFrame += 1;

                    if (enemy.dx == 1)
                    {
                        if (enemy.currentFrame >= enemy.walkRight.Count) enemy.currentFrame = 0;
                        if (enemy.walkRight.Count > 0) enemy.img = enemy.walkRight[enemy.currentFrame];
                        if (enemy.X >= enemyMaxX) enemy.dx = -1;
                    }
                    else
                    {
                        if (enemy.currentFrame >= enemy.walkLeft.Count) enemy.currentFrame = 0;
                        if (enemy.walkLeft.Count > 0) enemy.img = enemy.walkLeft[enemy.currentFrame];
                        if (enemy.X <= enemyMinX) enemy.dx = 1;
                    }
                }
                else if (enemyState == STATE_FIGHT)
                {
                    enemy.currentFrame += 1;
                    List<Bitmap> currentFightList = new List<Bitmap>();
                    if (enemy.dx == 1) currentFightList = enemy.fightRight;
                    else currentFightList = enemy.fightLeft;

                    if (enemy.currentFrame >= currentFightList.Count) enemy.currentFrame = 0;
                    if (currentFightList.Count > 0) enemy.img = currentFightList[enemy.currentFrame];

                    // Spawn bullet on frame 2 of shoot animation, with cooldown
                    if (enemy.currentFrame == 2 && enemyShootCooldown == 0)
                    {
                        CActor eb = new CActor();
                        eb.W = enemyBulletFrames.Count > 0 ? enemyBulletFrames[0].Width * 2 : 20;
                        eb.H = enemyBulletFrames.Count > 0 ? enemyBulletFrames[0].Height * 2 : 20;
                        eb.dx = enemy.dx; // fires toward hero
                        eb.X = enemy.X + (enemy.dx == 1 ? enemy.W : -eb.W);
                        eb.Y = enemy.Y + (enemy.H / 2) - (eb.H / 2);
                        eb.currentFrame = 0;
                        enemyBullets.Add(eb);
                        enemyShootCooldown = 60; // ~1.2 sec between shots
                    }

                    if (enemyShootCooldown > 0) enemyShootCooldown--;
                }
            }

            if (currentLevel == 2)
            {
                for (int i = 0; i < goblins.Count; i++)
                {
                    CActor gob = goblins[i];
                    int gobScreenX = ((gob.X - camX) * this.ClientSize.Width) / zoomWidth;

                    if (hero.X >= gobScreenX - 500 && hero.X <= gobScreenX + 500)
                    {
                        if (hero.X < gobScreenX) gob.dx = -1;
                        else gob.dx = 1;
                        gob.X += gob.dx * 6;
                    }
                    else
                    {
                        gob.X += gob.dx * 3;
                        if (gob.X > pitStart - gob.W) gob.dx = -1;
                        if (gob.X < 0) gob.dx = 1;
                    }

                    gob.currentFrame++;

                    if (gob.dx == 1)
                    {
                        if (gob.currentFrame >= goblinWalkR.Count) gob.currentFrame = 0;
                        if (goblinWalkR.Count > 0) gob.img = goblinWalkR[gob.currentFrame];
                    }
                    else
                    {
                        if (gob.currentFrame >= goblinWalkL.Count) gob.currentFrame = 0;
                        if (goblinWalkL.Count > 0) gob.img = goblinWalkL[gob.currentFrame];
                    }

                    if (hero.X + hero.W >= gobScreenX && hero.X <= gobScreenX + gob.W &&
                        hero.Y + hero.H >= gob.Y && hero.Y <= gob.Y + gob.H)
                    {
                        if (heroDamageCooldown == 0)
                        {
                            heroHealth -= 15;
                            heroDamageCooldown = 40;
                            isJumping = true;
                            verticalVelocity = -15;

                            if (hero.X < gobScreenX) hero.X -= 50;
                            else hero.X += 50;

                            if (heroHealth <= 0)
                            {
                                heroHealth = 0;
                                LoadLevel();
                                return;
                            }
                        }
                    }
                }
            }

            // Level 3 Boss - Fixed Laser AI
            if (currentLevel == 3)
            {
                Random r = new Random();
                foreach (CActor t in turrets)
                {
                    int tScreenX = ((t.X - camX) * this.ClientSize.Width) / zoomWidth;

                    if (tScreenX > -t.W && tScreenX < this.ClientSize.Width + 500)
                    {
                        if (t.currentFrame == 0) // Idle
                        {
                            if (r.Next(0, 100) < 2)
                            {
                                t.currentFrame = 1; // Begin charging
                                t.dx = 0;
                            }
                        }
                        else if (t.currentFrame == 1) // Charging (Warning beam visible)
                        {
                            t.dx++;
                            if (t.dx > 40)
                            {
                                t.currentFrame = 2; // Begin firing
                                t.dx = 0;
                            }
                        }
                        else if (t.currentFrame == 2) // Firing - straight beam at eye height
                        {
                            t.dx++;

                            int eyeY = t.Y + (int)(t.H * 0.57);

                            if (hero.Y < eyeY + 8 && hero.Y + hero.H > eyeY - 8)
                            {
                                if (heroDamageCooldown == 0)
                                {
                                    heroHealth -= 20;
                                    heroDamageCooldown = 40;

                                    if (heroHealth <= 0)
                                    {
                                        heroHealth = 0;
                                        LoadLevel();
                                        return;
                                    }
                                }
                            }

                            if (t.dx > 80)
                            {
                                t.currentFrame = 0;
                                t.dx = 0;
                            }
                        }
                    }
                }
            }
        }

        void CheckCollisions()
        {
            if (currentLevel == 1 && isEnemyAlive == true)
            {
                int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= enemyScreenX && hero.X <= enemyScreenX + enemy.W &&
                    hero.Y + hero.H >= enemy.Y && hero.Y <= enemy.Y + enemy.H)
                {
                    if (isJumping == true && verticalVelocity > 0)
                    {
                        enemy.health -= 1;
                        verticalVelocity = -25;
                        if (enemy.health <= 0) DropKey(enemy);
                    }
                    else if (heroDamageCooldown == 0 && enemyState == STATE_FIGHT)
                    {
                        heroHealth -= 25;
                        heroDamageCooldown = 40;

                        verticalVelocity = -15;
                        isJumping = true;

                        if (hero.X < enemyScreenX) hero.X -= 50;
                        else hero.X += 50;

                        if (heroHealth <= 0) LoadLevel();
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

        void HandleLevelTransitions()
        {
            if (currentLevel == 1)
            {
                int elevatorScreenX = ((elevator.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= elevatorScreenX && hero.X <= elevatorScreenX + elevator.W)
                {
                    if (hasKey == true) isRidingElevator = true;
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
            else if (currentLevel == 2)
            {
                int portalScreenX = ((portal.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + (hero.W / 2) >= portalScreenX && hero.X + (hero.W / 2) <= portalScreenX + portal.W && hasKey)
                {
                    portalTimer++;
                    if (portalTimer > 35)
                    {
                        currentLevel = 3;
                        LoadLevel();
                    }
                }
                else
                {
                    portalTimer = 0;
                }
            }
        }

        void LoadLevel()
        {
            if (currentLevel == 1)
            {
                bgImg = new Bitmap("Battleground1.png");

                zoomWidth = bgImg.Width / 2;
                zoomHeight = (zoomWidth * this.ClientSize.Height) / this.ClientSize.Width;

                elevator.W = 150;
                elevator.H = 30;
                elevator.X = bgImg.Width - 400;

                ladder.W = 80;
                ladder.H = 300;
                ladder.X = bgImg.Width / 3;

                movingPlatform.X = -1000;
            }
            else if (currentLevel == 2)
            {
                bgImg = new Bitmap("Level2_Background.png");

                zoomWidth = bgImg.Width / 2;
                zoomHeight = (zoomWidth * this.ClientSize.Height) / this.ClientSize.Width;

                elevator.X = -1000;
                isRidingElevator = false;
                ladder.X = -1000;
            }
            else if (currentLevel == 3)
            {
                bgImg = new Bitmap("Level3_Background.png");

                zoomHeight = bgImg.Height;
                zoomWidth = (zoomHeight * this.ClientSize.Width) / this.ClientSize.Height;

                elevator.X = -1000;
                isRidingElevator = false;
                ladder.X = -1000;
            }

            maxCamX = Math.Max(0, bgImg.Width - zoomWidth);
            camY = bgImg.Height - zoomHeight;

            InitHeroSprites();
            InitEnemySprites();

            hero.img = hero.walkRight[0];
            int scale = 4;
            hero.W = hero.img.Width * scale;
            hero.H = hero.img.Height * scale;

            groundY = this.ClientSize.Height - hero.H - 120;
            hero.Y = groundY;

            heroHealth = 100;
            heroDamageCooldown = 0;
            hasKey = false;
            isKeyDropped = false;
            heroBullets.Clear();
            enemyBullets.Clear();
            enemyShootCooldown = 0;

            if (currentLevel == 1)
            {
                elevator.Y = groundY + hero.H;
                ladder.Y = groundY - ladder.H + hero.H;

                enemy.health = 6;
                isEnemyAlive = true;

                if (enemy.walkLeft.Count > 0) enemy.img = enemy.walkLeft[0];
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
                hero.dx = 1;
            }
            else if (currentLevel == 2)
            {
                camX = maxCamX;
                hero.X = this.ClientSize.Width - 200;
                hero.img = hero.walkLeft[0];
                hero.dx = -1;

                int mapCenter = bgImg.Width / 2;
                pitStart = mapCenter - 250;
                pitEnd = mapCenter + 250;

                movingPlatform.W = 150;
                movingPlatform.H = 30;
                movingPlatform.X = pitEnd;
                movingPlatform.Y = groundY + hero.H - 10;
                platformDir = -1;
                isPlatformMoving = false;

                InitPortalSprites();
                portal.W = 180;
                portal.H = 260;
                portal.X = 5;
                portal.Y = groundY + hero.H - portal.H;
                portalTimer = 0;
                portal.currentFrame = 0;

                goblins.Clear();
                Random rand = new Random();
                for (int i = 0; i < 4; i++)
                {
                    CActor gob = new CActor();
                    if (goblinWalkR.Count > 0)
                    {
                        gob.img = goblinWalkR[0];
                        gob.W = gob.img.Width / 4;
                        gob.H = gob.img.Height / 4;
                    }

                    gob.X = pitStart - 100 - rand.Next(100, 600);
                    gob.Y = (groundY + hero.H) - gob.H;
                    gob.health = 2;
                    gob.dx = 1;

                    goblins.Add(gob);
                }
            }
            else if (currentLevel == 3)
            {
                camX = 0;

                hero.X = 100;
                hero.dx = 1;
                if (hero.walkRight.Count > 0) hero.img = hero.walkRight[0];

                turrets.Clear();

                CActor boss = new CActor();

                boss.W = 100;
                boss.H = 150;

                // FIXED: Anchored to the world image, not the screen. 
                // (If it's slightly off-center from the tree, tweak the 550)
                boss.X = bgImg.Width - 550;

                // Obelisk sits on the ground
                boss.Y = groundY + 150;

                boss.health = 10;
                boss.currentFrame = 0;
                boss.dx = 0;

                turrets.Add(boss);
            }

            isLoad = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isIntro)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    isIntro = false;
                    LoadLevel();
                }
                return;
            }

            if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0) { currentLevel = 2; LoadLevel(); return; }
            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3) { currentLevel = 3; LoadLevel(); return; }

            if (isLoad == true)
            {
                int ladderScreenX = ((ladder.X - camX) * this.ClientSize.Width) / zoomWidth;

                if (hero.X + hero.W >= ladderScreenX && hero.X <= ladderScreenX + ladder.W && currentLevel == 1)
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

                if (e.KeyCode == Keys.R && currentLevel == 3)
                {
                    isFiringLaser = true;
                }

                if (e.KeyCode == Keys.F && isClimbing == false && heroState == 0)
                {
                    heroState = 1;
                    isMultipleShooting = false;
                    hero.currentFrame = 0;
                }

                if (e.KeyCode == Keys.E && isClimbing == false && heroState == 0)
                {
                    heroState = 1;
                    isMultipleShooting = true;
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
            if (isIntro)
            {
                DrawDubb(this.CreateGraphics());
                return;
            }

            if (isLoad == true)
            {
                if (heroDamageCooldown > 0)
                {
                    heroDamageCooldown--;
                }

                if (currentLevel == 2)
                {
                    if (isPlatformMoving)
                    {
                        int platformWorldSpeed = 15;
                        movingPlatform.X += platformDir * platformWorldSpeed;

                        if (movingPlatform.X <= pitStart - movingPlatform.W)
                        {
                            isPlatformMoving = false;
                        }
                    }

                    if (portalFrames.Count > 0)
                    {
                        portalAnimTick++;
                        if (portalAnimTick > 5)
                        {
                            portalAnimTick = 0;
                            portal.currentFrame++;
                            if (portal.currentFrame >= portalFrames.Count) portal.currentFrame = 0;
                        }
                    }
                }

                UpdateHeroPhysics();
                UpdateHeroShooting();
                HandleLevelTransitions();
                UpdateEnemyAI();
                CheckCollisions();

                if (isFiringLaser) isFiringLaser = false;

                if (hero.Y > this.ClientSize.Height + 500)
                {
                    heroHealth = 0;
                    LoadLevel();
                    return;
                }

                DrawDubb(this.CreateGraphics());
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (isLoad == false && !isIntro)
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
                g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g2.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                DrawScene(g2);
                g.DrawImage(off, 0, 0);
            }
        }

        void DrawScene(Graphics g2)
        {
            g2.Clear(Color.Black);

            if (isWin)
            {
                string winText = "WINNER WINNER\n7AMDY EL A3RAG FOR DINNER";
                Font winFont = new Font("Courier New", 48, FontStyle.Bold);
                string[] lines = winText.Split('\n');
                float totalHeight = lines.Length * winFont.GetHeight(g2);
                float startY = (this.ClientSize.Height - totalHeight) / 2;
                foreach (string line in lines)
                {
                    SizeF lineSize = g2.MeasureString(line, winFont);
                    g2.DrawString(line, winFont, Brushes.White, (this.ClientSize.Width - lineSize.Width) / 2, startY);
                    startY += winFont.GetHeight(g2);
                }
                return;
            }

            if (isIntro)
            {
                if (introBg != null)
                    g2.DrawImage(introBg, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                g2.FillRectangle(new SolidBrush(Color.FromArgb(150, 0, 0, 0)), 0, 0, this.ClientSize.Width, this.ClientSize.Height);

                string title = "WELCOME TO 7AMDY EK";
                Font titleFont = new Font("Courier New", 72, FontStyle.Bold);
                SizeF titleSize = g2.MeasureString(title, titleFont);
                g2.DrawString(title, titleFont, Brushes.Gold, (this.ClientSize.Width - titleSize.Width) / 2, this.ClientSize.Height / 3);

                Rectangle playBtn = new Rectangle(this.ClientSize.Width / 2 - 100, this.ClientSize.Height / 2, 200, 60);
                g2.FillRectangle(Brushes.DarkOrange, playBtn);
                g2.DrawRectangle(Pens.White, playBtn);

                string btnText = "PLAY";
                Font btnFont = new Font("Courier New", 24, FontStyle.Bold);
                SizeF btnSize = g2.MeasureString(btnText, btnFont);
                g2.DrawString(btnText, btnFont, Brushes.White, playBtn.X + (playBtn.Width - btnSize.Width) / 2, playBtn.Y + (playBtn.Height - btnSize.Height) / 2);

                string instructions = "Click PLAY or Press ENTER";
                Font instFont = new Font("Courier New", 14, FontStyle.Italic);
                SizeF instSize = g2.MeasureString(instructions, instFont);
                g2.DrawString(instructions, instFont, Brushes.LightGray, (this.ClientSize.Width - instSize.Width) / 2, playBtn.Y + 80);

                return;
            }

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

                if (isEnemyAlive == true && enemy.img != null)
                {
                    int enemyScreenX = ((enemy.X - camX) * this.ClientSize.Width) / zoomWidth;
                    g2.DrawImage(enemy.img, enemyScreenX, enemy.Y, enemy.W, enemy.H);
                    g2.FillRectangle(Brushes.Red, enemyScreenX, enemy.Y + 20, (enemy.W / 6) * enemy.health, 10);
                }

                // Draw enemy bullets
                foreach (CActor eb in enemyBullets)
                {
                    int ebScreenX = ((eb.X - camX) * this.ClientSize.Width) / zoomWidth;
                    if (eb.img != null)
                        g2.DrawImage(eb.img, ebScreenX, eb.Y, eb.W, eb.H);
                    else
                        g2.FillEllipse(Brushes.OrangeRed, ebScreenX, eb.Y, 16, 16);
                }
            }

            if (currentLevel == 2)
            {
                int portalScreenX = ((portal.X - camX) * this.ClientSize.Width) / zoomWidth;
                if (portalFrames.Count > 0)
                {
                    g2.DrawImage(portalFrames[portal.currentFrame], portalScreenX, portal.Y, portal.W, portal.H);
                }
                else
                {
                    g2.FillRectangle(Brushes.DarkViolet, portalScreenX, portal.Y, portal.W, portal.H);
                    g2.DrawRectangle(new Pen(Color.Black, 8), portalScreenX, portal.Y, portal.W, portal.H);
                }

                int floorY = groundY + hero.H;
                int pitStartScreenX = ((pitStart - camX) * this.ClientSize.Width) / zoomWidth;
                int pitEndScreenX = ((pitEnd - camX) * this.ClientSize.Width) / zoomWidth;

                g2.FillRectangle(Brushes.OrangeRed, pitStartScreenX, floorY, pitEndScreenX - pitStartScreenX, this.ClientSize.Height - floorY);
                g2.FillRectangle(Brushes.Yellow, pitStartScreenX, floorY, pitEndScreenX - pitStartScreenX, 10);

                int platformScreenX = ((movingPlatform.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.FillRectangle(Brushes.DarkOrange, platformScreenX, movingPlatform.Y, movingPlatform.W, movingPlatform.H);
                g2.DrawRectangle(Pens.White, platformScreenX, movingPlatform.Y, movingPlatform.W, movingPlatform.H);

                foreach (CActor gob in goblins)
                {
                    if (gob.img != null)
                    {
                        int gobScreenX = ((gob.X - camX) * this.ClientSize.Width) / zoomWidth;
                        g2.DrawImage(gob.img, gobScreenX, gob.Y, gob.W, gob.H);

                        g2.FillRectangle(Brushes.Red, gobScreenX, gob.Y - 10, (gob.W / 2) * gob.health, 5);
                    }
                }
            }

            if (currentLevel == 3)
            {
                foreach (CActor t in turrets)
                {
                    int tScreenX = ((t.X - camX) * this.ClientSize.Width) / zoomWidth;

                    // --- Draw obelisk body from stacked rectangles ---
                    // Base / plinth
                    g2.FillRectangle(Brushes.DarkSlateGray, tScreenX - 10, t.Y + t.H - 14, t.W + 20, 14);
                    g2.DrawRectangle(Pens.Gray, tScreenX - 10, t.Y + t.H - 14, t.W + 20, 14);

                    // Lower block
                    g2.FillRectangle(new SolidBrush(Color.FromArgb(60, 34, 139)), tScreenX + 5, t.Y + (int)(t.H * 0.72), t.W - 10, (int)(t.H * 0.28));
                    g2.DrawRectangle(new Pen(Color.MediumPurple, 1), tScreenX + 5, t.Y + (int)(t.H * 0.72), t.W - 10, (int)(t.H * 0.28));

                    // Mid block
                    g2.FillRectangle(new SolidBrush(Color.FromArgb(60, 34, 139)), tScreenX + 10, t.Y + (int)(t.H * 0.44), t.W - 20, (int)(t.H * 0.30));
                    g2.DrawRectangle(new Pen(Color.MediumPurple, 1), tScreenX + 10, t.Y + (int)(t.H * 0.44), t.W - 20, (int)(t.H * 0.30));

                    // Upper block
                    g2.FillRectangle(new SolidBrush(Color.FromArgb(60, 34, 139)), tScreenX + 15, t.Y + (int)(t.H * 0.18), t.W - 30, (int)(t.H * 0.28));
                    g2.DrawRectangle(new Pen(Color.MediumPurple, 1), tScreenX + 15, t.Y + (int)(t.H * 0.18), t.W - 30, (int)(t.H * 0.28));

                    // Crystal tip (ellipse)
                    g2.FillEllipse(new SolidBrush(Color.FromArgb(200, 200, 100, 230)), tScreenX + 20, t.Y, t.W - 40, 28);
                    g2.DrawEllipse(new Pen(Color.Violet, 1), tScreenX + 20, t.Y, t.W - 40, 28);

                    // --- Eye (glowing orb in the mid block) ---
                    int eyeX = tScreenX + (t.W / 2);
                    int eyeY = t.Y + (int)(t.H * 0.57); // sits in the mid block

                    if (t.currentFrame == 0) // Idle eye - dim purple
                    {
                        g2.FillEllipse(new SolidBrush(Color.FromArgb(80, 50, 0, 80)), eyeX - 12, eyeY - 12, 24, 24);
                        g2.FillEllipse(new SolidBrush(Color.FromArgb(60, 34, 139)), eyeX - 7, eyeY - 7, 14, 14);
                    }
                    else if (t.currentFrame == 1) // Charging eye - bright green
                    {
                        g2.FillEllipse(new SolidBrush(Color.FromArgb(80, 0, 100, 0)), eyeX - 16, eyeY - 16, 32, 32);
                        g2.FillEllipse(Brushes.LimeGreen, eyeX - 10, eyeY - 10, 20, 20);
                        g2.FillEllipse(Brushes.White, eyeX - 4, eyeY - 4, 8, 8);

                        // Faint warning beam at eye height
                        g2.DrawLine(new Pen(Color.FromArgb(100, 50, 205, 50), 2), eyeX, eyeY, 0, eyeY);
                    }
                    else if (t.currentFrame == 2) // Firing eye - blazing
                    {
                        g2.FillEllipse(new SolidBrush(Color.FromArgb(100, 0, 120, 0)), eyeX - 20, eyeY - 20, 40, 40);
                        g2.FillEllipse(Brushes.LimeGreen, eyeX - 12, eyeY - 12, 24, 24);
                        g2.FillEllipse(Brushes.White, eyeX - 5, eyeY - 5, 10, 10);

                        // Death beam - straight from eye, no sweep
                        g2.DrawLine(new Pen(Color.FromArgb(60, 50, 205, 50), 14), eyeX, eyeY, 0, eyeY);
                        g2.DrawLine(new Pen(Color.LimeGreen, 6), eyeX, eyeY, 0, eyeY);
                        g2.DrawLine(new Pen(Color.White, 2), eyeX, eyeY, 0, eyeY);
                    }

                    // Health bar above obelisk
                    g2.FillRectangle(Brushes.DarkRed, tScreenX, t.Y - 18, t.W, 8);
                    g2.FillRectangle(Brushes.LimeGreen, tScreenX, t.Y - 18, (t.W * t.health) / 10, 8);
                    g2.DrawRectangle(Pens.White, tScreenX, t.Y - 18, t.W, 8);
                }
            }

            if (isKeyDropped == true)
            {
                int keyScreenX = ((keyDrop.X - camX) * this.ClientSize.Width) / zoomWidth;
                g2.FillRectangle(Brushes.Gold, keyScreenX, keyDrop.Y, keyDrop.W, keyDrop.H);
            }

            if (hero.img != null)
            {
                if (heroDamageCooldown == 0 || heroDamageCooldown % 4 < 2)
                {
                    g2.DrawImage(hero.img, hero.X, hero.Y, hero.W, hero.H);
                }
            }

            g2.DrawString("HERO HP", new Font("Courier New", 12, FontStyle.Bold), Brushes.White, 20, 10);
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
                g2.DrawString("Key Acquired", new Font("Courier New", 12), Brushes.White, 90, 60);
            }

            if (currentLevel == 2 && portalTimer > 0)
            {
                int alpha = Math.Min(255, portalTimer * 8);
                g2.FillRectangle(new SolidBrush(Color.FromArgb(alpha, 128, 0, 128)), 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
        }
    }
}