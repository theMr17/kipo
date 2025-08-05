using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class CharacterSelectUi : MonoBehaviour {
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Awake() {
        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }

    private void Start() {
        Lobby lobby = LobbyManager.Instance.GetLobby();

        lobbyCodeText.text = "Lobby Code: " + lobby.LobbyCode;
    }
}
