using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine{
public class Health_Enemy1 : Health{
	[Header("add")]
	public string hitObjectName;
	public int meleeScore;
	public int shotScore;

	public override void Damage(int damage, GameObject instigator, float flickerDuration,
		float invincibilityDuration, Vector3 damageDirection)
	{
		//ダメージを与えたobjectをstringにキャスト
		hitObjectName = instigator.ToString();
		Debug.Log(hitObjectName);

		if (damage <= 0)
		{
			OnHitZero?.Invoke();
			return;
		}

		// if the object is invulnerable, we do nothing and exit
		if (TemporaryInvulnerable || Invulnerable || ImmuneToDamage)
		{
			OnHitZero?.Invoke();
			return;
		}
		
		if (!this.enabled)
		{
			return;
		}

		// if we're already below zero, we do nothing and exit
		if ((CurrentHealth <= 0) && (InitialHealth != 0))
		{
			return;
		}

		// we decrease the character's health by the damage
		float previousHealth = CurrentHealth;
		CurrentHealth -= damage;

		LastDamage = damage;
		LastDamageDirection = damageDirection;
		OnHit?.Invoke();

		if (CurrentHealth < 0)
		{
			CurrentHealth = 0;
		}

		// we prevent the character from colliding with Projectiles, Player and Enemies
		if (invincibilityDuration > 0)
		{
			DamageDisabled();
			StartCoroutine(DamageEnabled(invincibilityDuration));
		}

		// we trigger a damage taken event
		MMDamageTakenEvent.Trigger(_character, instigator, CurrentHealth, damage, previousHealth);

		if (_animator != null)
		{
			_animator.SetTrigger("Damage");
		}

		// we play the damage feedback
		if (FeedbackIsProportionalToDamage)
		{
			DamageFeedbacks?.PlayFeedbacks(this.transform.position, damage);    
		}
		else
		{
			DamageFeedbacks?.PlayFeedbacks(this.transform.position);
		}

		if (FlickerSpriteOnHit)
		{
			// We make the character's sprite flicker
			if (_renderer != null)
			{
				StartCoroutine(MMImage.Flicker(_renderer, _initialColor, FlickerColor, 0.05f, flickerDuration));
			}
		}

		// we update the health bar
		UpdateHealthBar(true);

		// if health has reached zero
		if (CurrentHealth <= 0)
		{
			// we set its health to zero (useful for the healthbar)
			CurrentHealth = 0;
			if (_character != null)
			{
				if (_character.CharacterType == Character.CharacterTypes.Player)
				{
					LevelManager.Instance.KillPlayer(_character);
					return;
				}
			}

			Kill();
		}
	}
		/// <summary>
		/// Kills the character, instantiates death effects, handles points, etc
		/// </summary>
		public override void Kill()
		{
			if (ImmuneToDamage)
			{
				return;
			}
			
			// we prevent further damage
			DamageDisabled();

			// instantiates the destroy effect
			DeathFeedbacks?.PlayFeedbacks();

			// Adds points if needed.
			if (PointsWhenDestroyed != 0)
			{
				//トドメ攻撃の判定
				if(hitObjectName == "Weapon_player2_melee 1(Clone)DamageArea (UnityEngine.GameObject)"){
//					CorgiEnginePointsEvent.Trigger(PointsMethods.Add, meleeScore);
					Debug.Log("melee");
				}else{
//					CorgiEnginePointsEvent.Trigger(PointsMethods.Add, shotScore);
					Debug.Log("shot");
				}
			}

			if (_animator != null)
			{
				_animator.SetTrigger("Death");
			}

			if (OnDeath != null)
			{
				OnDeath();
			}

			// if we have a controller, removes collisions, restores parameters for a potential respawn, and applies a death force
			if (_controller != null)
			{
				// we make it ignore the collisions from now on
				if (CollisionsOffOnDeath)
				{
					_controller.CollisionsOff();
					if (_collider2D != null)
					{
						_collider2D.enabled = false;
					}
				}

				// we reset our parameters
				_controller.ResetParameters();

				if (GravityOffOnDeath)
				{
					_controller.GravityActive(false);
				}

				// we reset our controller's forces on death if needed
				if (ResetForcesOnDeath)
				{
					_controller.SetForce(Vector2.zero);
				}

				// we apply our death force
				if (ApplyDeathForce)
				{
					_controller.GravityActive(true);
					_controller.SetForce(DeathForce);
				}
			}


			// if we have a character, we want to change its state
			if (_character != null)
			{
				// we set its dead state to true
				_character.ConditionState.ChangeState(CharacterStates.CharacterConditions.Dead);
				_character.Reset();

				// if this is a player, we quit here
				if (_character.CharacterType == Character.CharacterTypes.Player)
				{
					return;
				}
			}

			if (DelayBeforeDestruction > 0f)
			{
				Invoke("DestroyObject", DelayBeforeDestruction);
			}
			else
			{
				// finally we destroy the object
				DestroyObject();
			}
		}
}
}