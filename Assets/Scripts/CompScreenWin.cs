using UnityEngine.UI;

public sealed class CompScreenWin : ScreenBase
{
	private Text _text;

	private void Awake()
	{
		_text = GetComponentInChildren<Text>();
	}

	private void OnEnable()
	{
		_text.text = $"Win '{DataShared.I.NameWinner}' with score: {DataShared.I.ScoreCurrent}";
	}
}