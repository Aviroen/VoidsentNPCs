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
    public enum State
    {
        Walking,
        Running,
        Sprinting
    }
    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> walkingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> runningAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame> sprintingAnimation = new List<FarmerSprite.AnimationFrame>();

    [XmlIgnore]
    public List<FarmerSprite.AnimationFrame>? locallyPlayingAnimation;

    public float randomStackOffset;

    [XmlIgnore]
    public int walkSpeed;

    [XmlIgnore]
    public NetEvent1Field<Vector2, NetVector2> attackedEvent = new NetEvent1Field<Vector2, NetVector2>();

    [XmlElement("leftDrift")]
    public readonly NetBool leftDrift = new NetBool();

    [XmlElement("specialNumber")]
    public readonly NetInt specialNumber = new NetInt();

    [XmlIgnore]
    public bool approachFarmer;

    [XmlIgnore]
    public Farmer? targettedFarmer;

    [XmlIgnore]
    public Vector2 velocity = Vector2.Zero;

    [XmlIgnore]
    public NetEnum<State> currentState = new NetEnum<State>();

    [XmlIgnore]
    public float stateTimer;

    public BBEG()
    {
        this.Initialize();
    }
    protected override void initNetFields()
    {
        base.initNetFields();
        base.NetFields.AddField(this.currentState, "currentState");
        this.attackedEvent.onEvent += OnAttacked;
    }
    public BBEG(Vector2 position)
        : base("Aviroen.Voidsent_Konryn", position)
    {
        if (Game1.random.NextBool())
        {
            this.leftDrift.Value = true;
        }
        this.Initialize();
        base.Slipperiness = 4;
        base.flip = Game1.random.NextDouble() < 0.45;
        base.HideShadow = false;
        this.Sprite.SpriteWidth = 64;
        this.Sprite.SpriteHeight = 128;
        this.stateTimer = Utility.RandomFloat(3f, 5f);
    }
    public BBEG(Vector2 position, GameLocation location)
        : base("Aviroen.Voidsent_Konryn", position)
    {
        this.randomStackOffset = Utility.RandomFloat(0f, 100f);
        base.flip = Game1.random.NextBool();
        this.specialNumber.Value = Game1.random.Next(100);
        if (Game1.MasterPlayer.mailReceived.Contains("Aviroen.Voidsent_ExcaliburMail"))
        {
            base.hasSpecialItem.Value = true;
            base.Health = 100000;
            base.DamageToFarmer *= 15;
        }
    }
    public virtual void Initialize()
    {
        base.HideShadow = false;
        this.walkingAnimation.AddRange(new FarmerSprite.AnimationFrame[]
        {
            new FarmerSprite.AnimationFrame(0, 1000),
            new FarmerSprite.AnimationFrame(1, 1000)
        });
        this.runningAnimation.AddRange(new FarmerSprite.AnimationFrame[]
        {
            new FarmerSprite.AnimationFrame(2, 500),
            new FarmerSprite.AnimationFrame(3, 500)
        });
        this.sprintingAnimation.AddRange(new FarmerSprite.AnimationFrame[]
        {
            new FarmerSprite.AnimationFrame(4, 250),
            new FarmerSprite.AnimationFrame(5, 250)
        });
    }
    public void PhaseOne()
    {

    }
    public void PhaseTwo()
    {

    }
    public void PhaseThree()
    {

    }
    protected override void updateAnimation(GameTime time)
    {
        base.updateAnimation(time);
        switch (this.currentState.Value)
        {
            case State.Walking:               
                this.PlayAnimation(this.walkingAnimation, loop: true);
                break;
            case State.Running:
                if (this.Health <= this.Health / 50)
                {
                    this.PlayAnimation(this.runningAnimation, loop: true);
                }
                break;
            case State.Sprinting:
                if (this.Health <= this.Health / 75)
                {
                    this.PlayAnimation(this.sprintingAnimation, loop: true);
                }
                break;
        }
        this.Sprite.animateOnce(time);
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
    public virtual void SetRandomMovement()
    {
        this.velocity = new Vector2((Game1.random.Next(2) != 1) ? 1 : (-1), (Game1.random.Next(2) != 1) ? 1 : (-1));
    }
    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
    }
    public virtual void OnAttacked(Vector2 trajectory)
    {
        if (Game1.IsMasterGame)
        {
            if (trajectory.LengthSquared() == 0f)
            {
                trajectory = new Vector2(04, -1f);
            }
            else
            {
                trajectory.Normalize();
            }
            trajectory *= 16f;
            BasicProjectile projectile = new BasicProjectile(base.DamageToFarmer / 3 * 2, 13, 3, 0, (float)Math.PI / 16f, trajectory.X, trajectory.Y, base.Position, null, null, null, explode: true, damagesMonsters: false, base.currentLocation, this);
            projectile.height.Value = 24f;
            projectile.ignoreMeleeAttacks.Value = true;
            projectile.hostTimeUntilAttackable = 0.1f;
            if (Game1.random.NextBool())
            {
                projectile.debuff.Value = "13";
            }
            base.currentLocation.projectiles.Add(projectile);
        }
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
            if (base.Health <= 0)
            {
                base.currentLocation.playSound("potterySmash");
            }
        }
        return actualDamage;
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
    public override void behaviorAtGameTick(GameTime time)
    {
        if (this.targettedFarmer == null || this.targettedFarmer.currentLocation != base.currentLocation)
        {
            this.targettedFarmer = null;
            this.targettedFarmer = this.findPlayer();
        }
        if (this.stateTimer > 0f)
        {
            this.stateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
            if (this.stateTimer <= 0f)
            {
                this.stateTimer = 0f;
            }
        }
        switch (this.currentState.Value)
        {
            case State.Walking:
                this.walkSpeed = 2;
                if (this.stateTimer == 0f)
                {
                    this.currentState.Value = State.Walking;
                    this.stateTimer = 1f;
                }
                break;
            case State.Running:
                this.walkSpeed = 3;
                if (this.stateTimer == 0f)
                {
                    if (this.TargetInRange())
                    {
                        this.currentState.Value = State.Running;
                        this.stateTimer = 1f;
                        this.walkSpeed = 4;
                    }
                    else
                    {
                        this.currentState.Value = State.Running;
                        this.stateTimer = 1f;
                    }
                }
                break;
            case State.Sprinting:
                this.walkSpeed = 4;
                if (this.stateTimer == 0f)
                {
                    if (this.TargetInRange())
                    {
                        this.currentState.Value = State.Sprinting;
                        this.stateTimer = 2;
                        this.walkSpeed = 5;
                    }
                    else
                    {
                        this.currentState.Value = State.Sprinting;
                        this.stateTimer = 1f;
                    }
                }
                break;
        }
        if (this.targettedFarmer != null && this.approachFarmer)
        {
            Point curTile = base.TilePoint;
            Point playerTile = this.targettedFarmer.TilePoint;
            if (curTile.X > playerTile.X)
            {
                this.velocity.X = -1f;
            }
            else if (curTile.X < playerTile.X)
            {
                this.velocity.X = 1f;
            }
            if (curTile.Y > playerTile.Y)
            {
                this.velocity.Y = -1f;
            }
            else if (curTile.Y < playerTile.Y)
            {
                this.velocity.Y = 1f;
            }
        }
        if (this.velocity.X != 0f || this.velocity.Y != 0f)
        {
            Rectangle next_bounds = this.GetBoundingBox();
            Vector2 next_position = base.Position;
            next_bounds.Inflate(48, 48);
            next_bounds.X += (int)this.velocity.X * this.walkSpeed;
            next_position.X += (int)this.velocity.X * this.walkSpeed;
            if (!this.TargetInRange())
            {
                this.velocity.X *= 1f;
                next_bounds.X += (int)this.velocity.X * this.walkSpeed;
                next_position.X += (int)this.velocity.X * this.walkSpeed;
            }
            next_bounds.Y += (int)this.velocity.Y * this.walkSpeed;
            next_position.Y += (int)this.velocity.Y * this.walkSpeed;
            if (!this.TargetInRange())
            {
                this.velocity.Y *= 1f;
                next_bounds.Y += (int)this.velocity.Y * this.walkSpeed;
                next_position.Y += (int)this.velocity.Y * this.walkSpeed;
            }
            if (base.Position != next_position)
            {
                base.Position = next_position;
            }
        }
    }
    public override void updateMovement(GameLocation location, GameTime time)
    {
    }
}
