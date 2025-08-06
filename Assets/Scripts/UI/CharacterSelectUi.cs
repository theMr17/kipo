using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class CharacterSelectUi : MonoBehaviour {
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private Button copyCodeButton;

    private void Awake()
    {
        readyButton.onClick.AddListener(() =>
        {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
        copyCodeButton.onClick.AddListener(() =>
        {
            Lobby lobby = LobbyManager.Instance.GetLobby();
            GUIUtility.systemCopyBuffer = lobby.LobbyCode;
        });
    }

    private void Start() {
        Lobby lobby = LobbyManager.Instance.GetLobby();

        lobbyCodeText.text = "Lobby Code: " + lobby.LobbyCode;
    }
}
