using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MasterSystem : MonoBehaviour {

	//ノーツCSV
	[SerializeField]
	private string noteCsv;

	//ノーツの速度
	[SerializeField]
	private float gameSpeed = 1;

	//ノーツ出現位置の最高点
	[SerializeField]
	private float noteHeight = 3;

	//何秒前にノーツが出現し始めるか
	[SerializeField]
	private float waitTime = 2;

	//フリックの判定の際どれだけ動いたら判定とするか
	[SerializeField]
	private float flickJudgeDistance = 60;

	//音楽のBpm
	[SerializeField]
	private float bpm = 252;

	//音ゲーをするBGMのオーディオ
	private AudioSource mainAudio;

	//ゲームシーン上で存在しているNoteDataを所持したオブジェクト
	private NoteData[] noteArray;

	//ノーツを拾うオブジェクトの位置とそれを参照する名前(NoteDataのposによって呼び出す)
	private Dictionary<string, Transform> noteTarget_D = new Dictionary<string, Transform>();

	private NoteAdd noteAdd;

	//現在の時間(今後、音楽の再生時間に変更)
	private float time = 0;

	void Awake(){
		Application.targetFrameRate = 80;
		TextAsset text = Resources.Load ("TextData/Test") as TextAsset;
		noteAdd = new NoteAdd (text);
	}

	void Start () {
		
		foreach (GameObject target in GameObject.FindGameObjectsWithTag("Target")) {
			noteTarget_D.Add (target.name, target.transform);
		}
//		noteArray = GameObject.FindObjectsOfType<NoteData> ();
		mainAudio = this.GetComponent<AudioSource> ();

//		Debug.LogFormat ("bps:{0} FrameLate:{1}", 60f/bpm, Application.targetFrameRate);
	}

	void Update () {
		NoteMover.NoteMove(noteAdd, noteTarget_D, waitTime, gameSpeed, noteHeight, mainAudio.time);
		FingerActionSelector.TapTrigger (noteAdd, flickJudgeDistance, mainAudio.time);

//		Debug.LogFormat ("{0}",1f/Time.deltaTime);
	}
}





//ノーツの移動に関するクラス
public class NoteMover:MonoBehaviour{
	public static void NoteMove (NoteAdd noteAdd, Dictionary<string, Transform> noteTarget_D,float waitTime,float gameSpeed,float noteHeight,float time) {
		foreach (GameObject note in GameObject.FindGameObjectsWithTag("Note")) {
			NoteData noteData = note.GetComponent<NoteData> ();

			if (noteData.time - time < 0) {
				NoteData nextNoteData = noteAdd.ChangeNoteData (noteData);
//				Debug.Log (noteData);
				if (nextNoteData.time <= -1) {
					Destroy (note);
					continue;
				} else {
					note.GetComponent<NoteData> ().NoteDataChange = nextNoteData;
				}
			}

			note.transform.position = new Vector3 (
				noteTarget_D [noteData.pos].position.x * (IndicateNote(noteData.time, noteData.time - ((waitTime * 1.67f) / gameSpeed), time)),
				(noteTarget_D [noteData.pos].position.y - noteHeight) * Mathf.Pow(((time - noteData.time + (waitTime / gameSpeed)) * (gameSpeed / waitTime)), 2) + noteHeight,
				noteData.transform.position.z);

			note.transform.localScale = noteTarget_D[noteData.pos].transform.localScale * (IndicateNote(noteData.time, noteData.time - ((waitTime * 1.67f) / gameSpeed), time));
		}
	}

	public static float IndicateNote(float max,float min,float ve){
		float range = max - min;
		float value = ve - min;

		if (ve <= min) {
			return 0;
		} else {
			return value / range;
		}
	}
}


//画面に触れている指の情報に関するクラス
public class FingerActionSelector{

	//TapTriggerメソッドで使用する現在画面に触れられている指の情報群(タップ位置,判定の実行非実行,ノーツターゲットの名前を所持)
	private static FingerActionData[] fad_A = {new FingerActionData(),new FingerActionData(),new FingerActionData(),new FingerActionData(),new FingerActionData()};

	public static void TapTrigger(NoteAdd noteAdd, float flickJudgeDistance, float time){
		
		foreach (Touch t in Input.touches) {
			var id = t.fingerId;

			string actionType = "";


			switch (t.phase) {
			case TouchPhase.Began:
				Ray ray = Camera.main.ScreenPointToRay (t.position);
				RaycastHit hit = new RaycastHit ();
				if (Physics.Raycast (ray, out hit, 100)) {
					//Debug.Log (hit.transform.name);
					fad_A [id].judgePos = hit.transform.name;
					fad_A [id].tapPos = Camera.main.WorldToScreenPoint (hit.transform.position);

					actionType = "tap";
				}

				break;
			
//			case TouchPha

			case TouchPhase.Moved:
				float xx = t.position.x - fad_A [id].tapPos.x;
				if (flickJudgeDistance < xx) {
//					右向きにフリック
//					Debug.Log ("右フリック");
					actionType = "right";

				} else if (-flickJudgeDistance > xx) {
//					左向きにフリック						
//					Debug.Log ("左フリック");
					actionType = "left";
				} else {
					actionType = "";
				}
				break;

			case TouchPhase.Ended:
				fad_A [id].wasJudge = false;
				actionType = "";
				break;
			}
			if (!fad_A [id].wasJudge && actionType != "") {
				fad_A [id].wasJudge = JudgeNote (noteAdd, fad_A [id].judgePos, actionType, time);
			}
		}
	}

	static bool JudgeNote(NoteAdd noteAdd, string targetName, string judgeType, float time){
		NoteData nearNote = serchNearNote (targetName, time);
		if (nearNote != null) {
//			Debug.LogFormat ("{0},{1}", judgeType, nearNote.type);
			if (judgeType == "tap" && nearNote.type == "tap") {
				if (Mathf.Abs (nearNote.time - time) <= 0.15f) {
					Debug.Log ("Good");

				} else if (Mathf.Abs(nearNote.time - time) <= 0.25f) {
					Debug.Log ("OK");

				} else {
					Debug.Log ("Bad");
				}
				noteAdd.ChangeNoteData (nearNote);
				return true;

			} else if (nearNote.type != "tap" && judgeType != "tap" && nearNote.type != judgeType) {
				Debug.Log ("ArrowBad");
				noteAdd.ChangeNoteData (nearNote);
				return true;

			} else if(nearNote.type == judgeType){
				if (Mathf.Abs (nearNote.time - time) <= 0.15f) {
					Debug.Log ("Good");

				} else if (Mathf.Abs(nearNote.time - time) <= 0.25f) {
					Debug.Log ("OK");

				} else {
					Debug.Log ("Bad");
				}
				noteAdd.ChangeNoteData (nearNote);
				return true;
			}
		}
		return false;
	}

	static NoteData serchNearNote(string targetName, float time){
		float tmpTime = 0;           //距離用一時変数
		float nearTime = 0;          //最も近いオブジェクトの距離
		NoteData targetObj = null; 		 //現在の判定を取るノーツオブジェクト
		bool firstJudge = true;		 //初回判定通過用bool

		//タグ指定されたオブジェクトを配列で取得する
		foreach (NoteData note in GameObject.FindObjectsOfType<NoteData> ()){
			if (targetName == note.pos && Mathf.Abs(note.time - time) <= 0.3f) {
				tmpTime = Mathf.Abs(time - note.time);
				//オブジェクトの距離が近いか、距離0であればオブジェクト名を取得
				//一時変数に距離を格納
				if (firstJudge || nearTime > tmpTime) {
					nearTime = tmpTime;
					targetObj = note;
					firstJudge = false;
				}
			}
		}
		return targetObj;
	}
}

public class NoteAdd:MonoBehaviour{

//	private static List<string[]> noteData_D = new List<string[]>();

	private static List<NoteData> noteData_D = new List<NoteData> ();

	public NoteAdd(TextAsset noteData){
		int height = 0;
		StringReader reader = new StringReader(noteData.text);

		while(reader.Peek() > -1) {
			string[] line = reader.ReadLine().Split(',');

			noteData_D.Add (new NoteData (line [0], float.Parse (line [1]), line [2]));

			height++; // 行数加算
		}

		for (int i = 0; i < 25; i++) {
			if (noteData_D.Count <= 0) {
				continue;
			} else {
				GameObject instantiateNote = Instantiate((GameObject)Resources.Load ("Prefabs/Note"));
				instantiateNote.tag = "Note";
				instantiateNote.transform.parent = GameObject.FindGameObjectWithTag("NoteFile").transform;
				instantiateNote.GetComponent<NoteData> ().NoteDataChange = noteData_D [0];
				noteData_D.RemoveAt (0);
			}
		}
	}

	public NoteData ChangeNoteData(NoteData note){
//		Debug.LogFormat ("First: noteData_D[0] = {0},note = {1}", noteData_D [0].time, note.time);
//		Debug.Log(noteData_D.Count);
		if (noteData_D.Count > 0) {
			NoteData noteData = noteData_D [0];
			noteData_D.RemoveAt (0);
			Debug.Log(noteData.time);
			return noteData;
		} else {
//			Destroy (note.gameObject);
			NoteData noteData = new NoteData("a",-1,"tap");
			return noteData;
		}
	}
}

//FingerActionSelectorで使用する現在画面に触れられている指の情報群(タップ位置,判定の実行非実行,ノーツターゲットの名前を所持)
public class FingerActionData{
	public Vector2 tapPos = new Vector2();

	public bool wasJudge = false;

	public string judgePos = "";
}