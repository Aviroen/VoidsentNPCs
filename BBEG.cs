using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using System.Xml.Serialization;
using StardewValley.Extensions;
using StardewValley.Projectiles;

namespace Voidsent.Monsters;
public class BBEG : Monster
{
    //CREATE SMOKE EFFECT FROM THE CAULDRON TO FOLLOW THE POSITION OF THE BOSS
    /*
     * make 3 separate methods corresponding to each phase
     * subdivide further for attacks
     * behaviorAtGameTick advance state if hp is below %
     * call relevant phase method
     * 8 frames per 50ms
     * don't tie behavior to animation
     */

    int healthDeciment = 0;

    //GreenSlime
    [XmlIgnore]
    public int readyToJump = -1;

    public int animateTimer;

    public int timeSinceLastJump;

    private readonly NetEvent1Field<Vector2, NetVector2> jumpEvent = new NetEvent1Field<Vector2, NetVector2>
    {
        InterpolationWait = false
    };

    //lavalurk
    public enum State
    {
        Walking,
        Idling,
        Lunging,
        Firing
    }
    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> walkingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> idlingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> lungingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> firingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame>? locallyPlayingAnimation;

    [XmlIgnore]
    public int walkSpeed;

    [XmlIgnore]
    public bool approachFarmer;

    [XmlIgnore]
    public Vector2 velocity = Vector2.Zero;

    [XmlIgnore]
    public NetEnum<State> currentState = new NetEnum<State>();

    [XmlIgnore]
    public Farmer? targettedFarmer;

    [XmlIgnore]
    public float stateTimer;

    [XmlIgnore]
    public float fireTimer;

    public BBEG()
    {
        this.Initialize();
    }
    protected override void initNetFields()
    {
        base.initNetFields();
    }
    public BBEG(Vector2 position)
        : base("Aviroen.Voidsent_Konryn", position)
    {
        this.Sprite.SpriteWidth = 64;
        this.Sprite.SpriteHeight = 128;
        this.Sprite.UpdateSourceRect();
        this.Initialize();
        base.ignoreDamageLOS.Value = true;
        base.Slipperiness = 4;
        this.stateTimer = Utility.RandomFloat(3f, 5f);
        base.HideShadow = false;
        base.Breather = true;
        base.flip = Game1.random.NextBool();
        base.objectsToDrop.Add("Aviroen.Voidsent_ITEMHERE");
        base.MaxHealth = 100000;
        base.DamageToFarmer *= 15;
        if (Game1.player.mailReceived.Contains("Aviroen.Voidsent_ExcaliburMail"))
        {
            base.Health = (int)(100000 - Game1.player.stats.Get("Aviroen.VoidsentCP_Fighters") * 2000);
        }
    }
    public virtual void Initialize()
    {
        base.HideShadow = false;
        this.walkingAnimation.AddRange(new FarmerSprite.AnimationFrame[4]
        {
            new FarmerSprite.AnimationFrame(0, 200),
            new FarmerSprite.AnimationFrame(1, 200),
            new FarmerSprite.AnimationFrame(2, 200),
            new FarmerSprite.AnimationFrame(3, 200)
        });
        this.idlingAnimation.AddRange(new FarmerSprite.AnimationFrame[4]
        {
            new FarmerSprite.AnimationFrame(4, 200),
            new FarmerSprite.AnimationFrame(5, 200),
            new FarmerSprite.AnimationFrame(6, 200),
            new FarmerSprite.AnimationFrame(7, 200)
        });
        this.lungingAnimation.AddRange(new FarmerSprite.AnimationFrame[4]
        {
            new FarmerSprite.AnimationFrame(8, 200),
            new FarmerSprite.AnimationFrame(9, 200),
            new FarmerSprite.AnimationFrame(10, 200),
            new FarmerSprite.AnimationFrame(11, 200)
        });
        this.firingAnimation.AddRange(new FarmerSprite.AnimationFrame[4]
        {
            new FarmerSprite.AnimationFrame(12, 200),
            new FarmerSprite.AnimationFrame(13, 200),
            new FarmerSprite.AnimationFrame(14, 200),
            new FarmerSprite.AnimationFrame(15, 200)
        });
    }
    public void PhaseOne(GameTime time)
    {
        /*
         * 1000ms reaction speed
         * shoots projectiles (non-heat seeking) at player
         * spin animation (touch damage)
         * lunge (slime shiver and lunge)
         */
        base.updateAnimation(time);
        switch (this.currentState.Value)
        {
            case State.Walking:
                this.PlayAnimation(this.walkingAnimation, loop: true);
                if (this.stateTimer == 0f)
                {
                    this.stateTimer = 1f;
                }
                break;
            case State.Lunging:
                this.PlayAnimation(this.lungingAnimation, loop: true);
                if (this.stateTimer == 0f)
                {
                    if (this.TargetInRange())
                    {
                        this.stateTimer = 1f;
                    }
                    else
                    {
                        this.stateTimer = 1f;
                    }
                }
                break;
            case State.Firing:
                this.PlayAnimation(this.firingAnimation, loop: true);
                if (this.stateTimer == 0f)
                {
                    this.stateTimer = 1f;
                }
                if (!(this.fireTimer > 0f))
                {
                    break;
                }
                this.fireTimer -= (float)time.ElapsedGameTime.TotalSeconds;
                if (this.fireTimer <= 0f)
                {
                    this.fireTimer = 0.25f;
                    if (this.targettedFarmer != null)
                    {
                        Vector2 shot_origin = base.Position + new Vector2(0f, -32f);
                        Vector2 shot_velocity = this.targettedFarmer.Position - shot_origin;
                        shot_velocity.Normalize();
                        shot_velocity *= 7f;
                        base.currentLocation.playSound("Aviroen.VoidsentCP_Shing");
                        BasicProjectile projectile = new BasicProjectile(25, 10, 0, 3, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, shot_origin, null, null, null, explode: false, damagesMonsters: false, base.currentLocation, this);
                        projectile.IgnoreLocationCollision = true;
                        projectile.ignoreTravelGracePeriod.Value = true;
                        projectile.maxTravelDistance.Value = 640;
                        base.currentLocation.projectiles.Add(projectile);
                    }
                }
                break;
            case State.Idling:
                this.PlayAnimation(this.idlingAnimation, loop: true);
                if (this.stateTimer == 0f)
                {
                    this.stateTimer = 1f;
                }
                break;
        }
        this.Sprite.animateOnce(time);
    }
    public void PhaseTwo(GameTime time)
    {
        /*
         * 500ms reaction speed
         * shoots projectiles at player
         * spin animation (touch damage)
         * lunge (slime shiver and lunge, further range)
         * hand slam animation
         */
        base.updateAnimation(time);
    }
    public void PhaseThree(GameTime time)
    {
        /*
         * 200ms reaction speed
         * shoots projectiles (non-heat seeking) at player
         * lunge (slime shiver and lunge, furthest range)
         * fist slam (extremely high damage)
         */
        base.updateAnimation(time);
    }
    public virtual bool PlayAnimation(List<FarmerSprite.AnimationFrame> animation_to_play, bool loop)
    {
        if (this.locallyPlayingAnimation != animation_to_play)
        {
            this.locallyPlayingAnimation = animation_to_play;
            this.Sprite.setCurrentAnimation(animation_to_play);
            this.Sprite.loop = loop;
            if (!loop)
            {
                this.Sprite.oldFrame = animation_to_play.Last().frame;
            }
            return true;
        }
        return false;
    }
    public virtual bool TargetInRange()
    {
        if (this.targettedFarmer == null)
        {
            return false;
        }
        if (Math.Abs(this.targettedFarmer.Position.X - base.Position.X) <= 640f && Math.Abs(this.targettedFarmer.Position.Y - base.Position.Y) <= 640f)
        {
            return true;
        }
        return false;
    }
    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
    }
    public override void reloadSprite(bool onlyAppearance = false)
    {
        this.Sprite = new AnimatedSprite("Characters\\Monsters\\Aviroen.Voidsent_Konryn");
    }
    public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
    {
        int actualDamage = Math.Max(1, damage - base.resilience.Value);
        if (Game1.random.NextDouble() < base.missChance.Value - base.missChance.Value * addedPrecision)
        {
            actualDamage = -1;
        }
        else
        {
            if (Game1.random.NextDouble() < 0.025)
            {
                if (!base.focusedOnFarmers)
                {
                    base.DamageToFarmer += base.DamageToFarmer / 2;
                    base.shake(1000);
                }
                base.focusedOnFarmers = true;
            }
            base.Slipperiness = 5;
            base.Health -= actualDamage;
            base.setTrajectory(xTrajectory, yTrajectory);
            base.currentLocation.playSound("clank");
            this.readyToJump = -1;
            base.IsWalkingTowardPlayer = true;
            if (base.Health <= 0)
            {
                base.currentLocation.playSound("potterySmash");
                base.deathAnimation();
            }
        }
        return actualDamage;
    }
    public override void onDealContactDamage(Farmer who)
    {
        if (Game1.random.NextDouble() < 0.3 && base.Player == Game1.player && !base.Player.temporarilyInvincible && Game1.random.Next(11) >= who.Immunity && !base.Player.hasTrinketWithID("BasiliskPaw"))
        {
            base.currentLocation.playSound("clank");
        }
        base.onDealContactDamage(who);
    }
    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);
        this.jumpEvent.Poll();
    }
    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);
        if (this.targettedFarmer == null || this.targettedFarmer.currentLocation != base.currentLocation)
        {
            this.targettedFarmer = null;
            this.targettedFarmer = this.findPlayer();
        }
        if (this.Health >= this.Health / 2)
        {
            this.PhaseOne(time);
        }
        if (this.Health <= this.Health / 2)
        {
            this.PhaseTwo(time);
            this.walkSpeed = 4;
        }
        if (this.Health <= this.Health / 4)
        {
            this.PhaseThree(time);
            this.walkSpeed = 6;
        }
    }
    public override void updateMovement(GameLocation location, GameTime time)
    {
    }
}
