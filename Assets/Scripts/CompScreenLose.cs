using UnityEngine.UI;

public sealed class CompScreenLose : ScreenBase
{
	private Text _text;

	private void Awake()
	{
		_text = GetComponentInChildren<Text>();
	}

	private void OnEnable()
	{
		_text.text = $"Lose with score: {DataShared.I.ScoreCurrent}, the winner is: '{DataShared.I.NameWinner}'";
	}
}
