using System.Linq;
using UnityEngine.UI;

public sealed class CompScreenMatch : ScreenBase
{
	private Text _text;
	private int _score;

	private void Awake()
	{
		_text = GetComponentsInChildren<Text>().First(_ => _.name.Contains("score"));
	}

	private void OnEnable()
	{
		_score = -1;
	}

	public void LateUpdate()
	{
		if(_score != DataShared.I.ScoreCurrent)
		{
			_text.text = $"Current score: {DataShared.I.ScoreCurrent}";
			_score = DataShared.I.ScoreCurrent;
		}
	}
}
