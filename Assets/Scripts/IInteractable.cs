using UnityEngine;

public interface IInteractable
{
   void interact();
   bool isInteractable(); 
}

public interface IInteractionSession
{
   bool IsInteractionActive { get; }
}
