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
    //GreenSlime
    [XmlIgnore]
    public int readyToJump = -1;

    public int animateTimer;

    public int timeSinceLastJump;

    private readonly NetEvent1Field<Vector2, NetVector2> jumpEvent = new NetEvent1Field<Vector2, NetVector2>
    {
        InterpolationWait = false
    };
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
        this.Initialize();
        base.Slipperiness = 4;
        base.HideShadow = false;
        this.Sprite.SpriteWidth = 64;
        this.Sprite.SpriteHeight = 128;
    }
    public BBEG(Vector2 position, GameLocation location)
        : base("Aviroen.Voidsent_Konryn", position)
    {
        base.flip = Game1.random.NextBool();
        if (Game1.MasterPlayer.mailReceived.Contains("Aviroen.Voidsent_ExcaliburMail"))
        {
            base.Health = 100000;
            base.DamageToFarmer *= 15;
            base.objectsToDrop.Add("Aviroen.Voidsent_ITEMHERE");
        }
    }
    public virtual void Initialize()
    {
        base.HideShadow = false;
    }
    public void PhaseOne()
    {
        /*
         * 1000ms reaction speed
         * shoots projectiles (non-heat seeking) at player
         * spin animation (touch damage)
         * lunge (slime shiver and lunge)
         */
    }
    public void PhaseTwo()
    {
        /*
         * 500ms reaction speed
         * shoots projectiles at player
         * spin animation (touch damage)
         * lunge (slime shiver and lunge, further range)
         * hand slam animation
         */
    }
    public void PhaseThree()
    {
        /*
         * 200ms reaction speed
         * shoots projectiles (non-heat seeking) at player
         * lunge (slime shiver and lunge, furthest range)
         * fist slam (extremely high damage)
         */
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
        if (this.Health >= this.Health / 2)
        {
            this.PhaseOne();
        }
        else if (this.Health <= this.Health / 2)
        {
            this.PhaseTwo();
        }
        else if (this.Health <= this.Health / 4)
        {
            this.PhaseThree();
        }
    }
    public override void updateMovement(GameLocation location, GameTime time)
    {
    }
}
