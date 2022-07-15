using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine;

//Objet pouvant être interactif avec les habiletés du joueur.

public interface ILevelChange
{
    public void ActivateDoor();
}

public interface ISaveable
{
    public void GetGuid();

    public void Save();

    public void Load();
}


public interface ICloseable
{
    public void Close();

    public void Open();
}

public interface IWindow
{
    public void Close();
}

public interface IBurnable
{
    public void Burn();

    public void LightUp();

    public void Extinguish();
}

public interface IMeltable
{
    public void Melt();

    public void Reform();

    public void StopMelt();
}

public interface IInteractable
{
    public void Interact();
    public void Selected(bool outline);

    public int GetMyID();
}

public interface ICollectible : ISaveable
{
    public void SelfDestruct();
}

public interface IMoveable : ISaveable
{
    public void GetTransform();

    public void MoveForward();
    public void MoveBack();
    public void MoveLeft();
    public void MoveRight();
}

public interface ITriggerable
{

    public void OnEnter();
    public void OnStay();
    public void OnExit();

    public void Activate();

    public void DeActivate();
}

public interface IBreakable
{
    public void Break(); 

}

public interface IFreezable
{
    public void Freeze();

    public void Unfreeze();

}

public interface IGrabable
{
    public void Grab();
}

public interface IVaultable
{
    public void VaultUp();
 
}

public interface ISqueezable
{
    public void Squeeze();
}

public interface IClimbable
{
    public void ClimbUpSamll();
    public void ClimbUpBig();
}

public interface ISmallClimbable
{
    public void SmallClimb();
}

public interface ITouchable
{
    public void HandOnTarget();
}

public interface IPickable
{
    public void PickLock();
    public void Unlock();
}

public interface IResetable
{
    public void ResetToLastState();
}

public interface ISoundPlayable
{
    public void PlaySound();
    public void StopSound();
}

public interface IPowerable
{
    public void Powered();

    public void UnPowered();
}
public interface IDamageable
{
    public void TakeDamage(int damageValue = 0);
}

public interface ISharpen
{
    public void Sharpen();
}

public interface ILootable
{
    public Transform PickUp();

    public int GetMyID();
}

public interface IDeepSnowable 
{
    public void SlowDown();

    public void BackToNormal();
}

public interface IShootable
{
    public void ShootDown();
}

public interface IPustule
{
    public void DestroyPustule(int id);
}
public interface IChest
{
    public void UseChest(int id);
}
public interface ILockedChest
{
    public void BreakLock();
}
