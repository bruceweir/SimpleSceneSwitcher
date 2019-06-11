using System.Collections;
using System.Collections.Generic;
using System.Net;
using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HTTPHandler : MonoBehaviour {

	private static HTTPHandler _instance;
	public static HTTPHandler Instance { get { return _instance;} }
	public int port = 80;
	private HttpListener httpListener;
	private Thread listenerThread;
	private GameObject taskExecutor;
	private TaskExecutorScript taskExecutorScript;
	
	void Awake() {

		if(_instance != null && _instance != this)
		{
			Destroy(this.gameObject);
		}
		else
		{
			_instance = this;
			DontDestroyOnLoad(this.gameObject);
		}
	}
	void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
	// called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GetSceneObjectReferences();
    }

   void GetSceneObjectReferences() {

        taskExecutor = GameObject.Find("TaskExecutor");
        taskExecutorScript = taskExecutor.GetComponent<TaskExecutorScript>();		
    }
	// Use this for initialization
	void Start () {

		if (!HttpListener.IsSupported) {
			Debug.Log("HttpListener not supported");
			return;
		}

		
		taskExecutor = GameObject.Find("TaskExecutor");
        taskExecutorScript = taskExecutor.GetComponent<TaskExecutorScript>();

		try
		{
			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://*:" + port.ToString() +"/");
			
			httpListener.Start();
			
			listenerThread = new Thread(StartListener);
			listenerThread.Start();
		
			Debug.Log("Server started");
		}
		catch(Exception e)
		{
			
		}	
	}
	
	private void StartListener () {

		
		while(true) {
			IAsyncResult result = httpListener.BeginGetContext(new AsyncCallback(ListenerAsync), httpListener);
		
			//Debug.Log("Waiting for request.");
			result.AsyncWaitHandle.WaitOne();
			//Debug.Log("Request processed asyncronously.");
		}

	}


	private void ListenerAsync(IAsyncResult result) {

		HttpListener listener = (HttpListener) result.AsyncState;
		// Call EndGetContext to complete the asynchronous operation.
		HttpListenerContext context = listener.EndGetContext(result);
		HttpListenerRequest request = context.Request;

		//Debug.Log("Method: " + context.Request.HttpMethod );
		Debug.Log("LocalURL: " + context.Request.Url.LocalPath );
		
		if(context.Request.HttpMethod == "GET") {
			StartResponseTask(context);
		}
		
	}

	private void StartResponseTask(HttpListenerContext context) {
			
		if(context.Request.Url.LocalPath == "/")
		{
			taskExecutorScript.ScheduleTask(new Task(delegate
			{
				SendResponse(context);
			}));

			return;
		}

		String sceneName = context.Request.Url.LocalPath.Replace("/", string.Empty);

		Debug.Log("StartResponseTask: " + sceneName);
		taskExecutorScript.ScheduleTask(new Task(delegate
		{
			SceneManager.LoadScene(sceneName);
			SendSceneSwitchResponse(context, sceneName);
		}));

	}

	private void SendResponse(HttpListenerContext context)
	{
		HttpListenerResponse response = context.Response;
		// Construct a response. This needs to be done on the main thread, which is why
		// this function is called using ScheduleTask on taskExecutorScript

		//Start a thread to send the response back to the user
		Thread sendThread = new System.Threading.Thread(new ThreadStart(delegate{
			
			string responseString = CreateSceneSummaryResponse();

			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
			output.Close();
		}));

		sendThread.Start();
	}


	private string CreateSceneSummaryResponse()
	{
	//	string responseString = "<HTML><BODY>"+sceneInfo+"</BODY></HTML>";
		
		StringBuilder sb = new StringBuilder();
		sb.Append("<HTML><HEAD>");
		//this prevents the browser from making a favicon.ico request
		sb.Append("<link rel=\"icon\" href=\"data:,\">");
		sb.Append("</HEAD>");
		sb.Append("<BODY>");
		sb.Append("<H1>Hello Chijioke!</H1>");
		sb.Append("<p><a href=\"scene1\">Scene1</a>");
		sb.Append("<p><a href=\"scene2\">Scene2</a>");
		sb.Append("<p><a href=\"scene3\">Scene3</a>");
		sb.Append("<p><a href=\"scene4\">Scene4</a>");
		sb.Append("</BODY></HTML>");

		return sb.ToString();
	}


	private void SendSceneSwitchResponse(HttpListenerContext context, string sceneName)
	{
		HttpListenerResponse response = context.Response;
		
		Thread sendThread = new System.Threading.Thread(new ThreadStart(delegate{
			
			string responseString = CreateSceneSwitchResponse(sceneName);

			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
			output.Close();
		}));

		sendThread.Start();
	}

	private string CreateSceneSwitchResponse(string sceneName)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("<HTML><HEAD>");
		sb.Append("<link rel=\"icon\" href=\"data:,\">"); //this prevents the browser from making a favicon.ico request
		sb.Append("</HEAD>");
		
		sb.Append("<BODY>");
		sb.Append("<H1>Scene switch</H1>");
		sb.Append("<H2>OK</H2>");
		sb.Append("Switched to scene <b>" + sceneName + "</b>");
		sb.Append("<p><a href=\"scene1\">Scene1</a>");
		sb.Append("<p><a href=\"scene2\">Scene2</a>");
		sb.Append("<p><a href=\"scene3\">Scene3</a>");
		sb.Append("<p><a href=\"scene4\">Scene4</a>");
		
		sb.Append("</BODY></HTML>");

		return sb.ToString();
	}
		
}
