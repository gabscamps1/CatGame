using System;
using CatGame.Core.Data;
using CatGame.Core.Interfaces;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour, IDisposable
{
    private IPlayerInputController inputController;

    public event Action OnThrow;

    public bool IsInitialized => inputController != null;

    public bool Initialize(PlayerId playerId, IInputService inputService, IPlayerRegistry playerRegistry)
    {
        if (IsInitialized)
            return true;

        if (!playerRegistry.TryGetPlayerInfo(playerId, out PlayerInfo playerInfo))
            return false;
        
        PlayerId inputId = new PlayerId(playerInfo.localIndex);

        inputController = inputService.GetInputFromPlayer(inputId);

        if (inputController == null)
            return false;

        inputController.OnThrow += HandleThrow;

        return true;
    }

    public Vector2 ReadMove()
    {
        if (inputController == null)
            return Vector2.zero;

        return inputController.Move.ReadValue<Vector2>();
    }

    private void HandleThrow()
    {
        OnThrow?.Invoke();
    }

    public void Dispose()
    {
        if (inputController == null)
            return;

        inputController.OnThrow -= HandleThrow;
        inputController = null;
    }

    private void OnDestroy()
    {
        Dispose();
    }
}