using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour {
  [SerializeField] private Button createLobbyButton;
  [SerializeField] private Button joinCodeButton;
  [SerializeField] private TMP_InputField joinCodeInputField;
  
  private void Awake()
  {
    joinCodeButton.onClick.AddListener(() =>
    {
      LobbyManager.Instance.JoinLobby(joinCodeInputField.text);
    });

    createLobbyButton.onClick.AddListener(() =>
    {
      LobbyManager.Instance.CreateLobby(lobbyName: "Dummy Lobby Name", isPrivate: true);
    });
  }
}
