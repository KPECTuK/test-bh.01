using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class CompMatch : ScreenBase
{
	private Text _text;
	private int _score = -1;

	private void Awake()
	{
		_text = GetComponentsInChildren<Text>().First(_ => _.name.Contains("score"));
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
