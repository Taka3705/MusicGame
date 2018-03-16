using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteData: MonoBehaviour {

	public NoteData(string targetPos, float targetTime, string targetType){
		time = targetTime;
		pos = targetPos;
		type = targetType;
	}

	public NoteData NoteDataChange{
		set{
			time = value.time;
			pos = value.pos;
			type = value.type;
		;}
	}

	//ノーツの落下位置(右からa)
	[SerializeField]
	public string pos = "a";

	//ターゲットに対してノーツが完璧に重なる時間
	[SerializeField]
	public float time = 0f;

	//ノーツの判定方法
	[SerializeField]
	public string type = "tap";
}
