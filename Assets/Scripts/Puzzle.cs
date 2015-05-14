﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using IronPython;
using IronPython.Modules;
using Microsoft.Scripting.Hosting;

// version of the overall python code update system that accounts for pre-loaded environment objects to setup for a puzzle
public class Puzzle : PythonInterpreter {
	public Text puzzleText;
	public Text puzzleTextRight;
	private int currLevel = 1;
	private bool levelCompleted = false; 
	public Material redSpaceSky;
	public Material defaultSky;
	public OVRScreenFade fade;
	private ObjectOperations operations;

	public void Awake() {
		codeStr = input.text;
		lastCodeStr = input.text;
		engine = IronPython.Hosting.Python.CreateEngine(); // setup python engine
		scope = engine.CreateScope();
		setupPythonEngine();
		source = engine.CreateScriptSourceFromFile("Assets/PythonPuzzles/Puzzle1.py"); 
		source.Execute(scope);
		input.text = "firstCube.setColor(red)";
		operations = engine.Operations;

	}

	GameObject getGameObjectForVar(string varName) {
		object obj;
		scope.TryGetVariable<object>(varName, out obj);
		if (obj == null) {
			return null;
		}
		if (obj.GetType() == typeof(IronPython.Runtime.Types.OldClass))
			return null;

		object method;
		PythonInterpreter.engine.Operations.TryGetMember(obj, "getObject", out method);  

		if (method == null) {
			return null;
		}
		GameObject instanceObj = (GameObject) PythonInterpreter.engine.Operations.Invoke(method);
		return instanceObj;
	}

	void resetWeather() {
		GameObject rain = GameObject.Find("Rain(Clone)");
		GameObject snow = GameObject.Find("Snow(Clone)");
		if (rain != null) {
			rain.SetActive(false);
		}
		if (snow != null) {
			snow.SetActive(false);
		}
	}

	void resetSkybox() {
		RenderSettings.skybox = defaultSky;
	}
	
	void resetScene() {
		input.text = "";
		puzzleTextRight.text = "";
		puzzleText.text = "";
		resetWeather(); //  makes sure weather doesn't persist after level reset
		resetSkybox(); //   makes sure skybox goes back to default
		source = engine.CreateScriptSourceFromFile("Assets/PythonPuzzles/Puzzle" + currLevel + ".py"); 
		source.Execute(scope);
		fade.startFade();
	}

	bool checkPuzzleComplete() {
		bool solved = false;
		if (currLevel == 1) {
			solved = checkLevel1Cond();
		}
		else if (currLevel == 2) {
			solved = checkLevel2Cond();
		}

		else if (currLevel == 3) {
			solved = checkLevel3Cond();
		}
		else if (currLevel == 4) {
			solved = checkLevel4Cond();
		}

		return solved;
	}

	bool checkLevel1Cond() {
		// Red cube has color green
		GameObject cube = getGameObjectForVar("firstCube");
		if (cube.GetComponent<Renderer>().material.color == Color.green) {
			return true;
		}
		return false;
	}

	bool checkLevel2Cond() {
		// check if the user changed the skybox

		if (RenderSettings.skybox == redSpaceSky) {
			return true;
		}
		return false;
	}

	bool checkLevel3Cond() {
		// check if the user changed the skybox
		object cube = scope.GetVariable("c2");

		string spinning = operations.GetMember(cube, "spinning").ToString();
		if (spinning == "True") {
			return true;
		}
		return false;
	}

	bool checkLevel4Cond() {
		// check if the user changed the skybox
		GameObject cube = getGameObjectForVar("c1");

		if (cube != null) {
			if (cube.name == "Cube") {
				return true;
			}
		}
		return false;
	}

	void displayLevelCompleted() {
		if (currLevel == 1) {
			puzzleText.text = "Great, you will get the hang of this in no time! \n\nPress F3 to move on.";
		}
		else if (currLevel == 2) {
			puzzleText.text = "Ooh, space! Try some of the other options for the sky and Press F3 to move on.";
		}
		else if (currLevel == 3) {
			puzzleTextRight.text = "";
			puzzleText.text = "Look at that cube go! Any object can be interacted with by using their methods in a similar way." +
				"as you have now seen from changing the sky and the cubes.\n\n As always, F3 to move along.";
		}
		else if (currLevel == 4) {
			puzzleTextRight.text = "";
			puzzleText.text = "Awesome! Now we can both create and interact with objects. " +
				"Objects you create are instances of the class you create them from.";
		}
	}

	void setupLevel2() {
		puzzleText.text = "The world is yours to control! Why don't we travel to a red planet? You have access to the sky! " +
			"\n\nTry using one of the methods that comes up for the Sky when you hit F1.";
		input.text = "sky.setCloudy()";
	}

	void setupLevel3() {
		puzzleText.text = "You are doing great so far! Now, remember how you interacted with the world in the past few tasks?" +
			"Time to do that again on your own.";
		puzzleTextRight.text = "It would be great if we could make the blue cube spin. " +
			"Hit F1 to find something that might help. Remember you can look at any object to see its name.";
	}

	void setupLevel4() {
		input.text = "s = Sphere(0, 3, -70, yellow)";
		puzzleText.text = "Now you know how to interact with objects. But how do you create them?";
		puzzleTextRight.text = "The sphere in front of you exists because of the code in front of you." +
			"Can you create a cube named c1 in the same way?";
	}

	void nextLevel() {
		currLevel += 1;
		levelCompleted = false;
		clearCreatedObjects();
		resetScene();

		if (currLevel == 2) {
			setupLevel2();
		}
		else if (currLevel == 3) {
			setupLevel3();
		}
		else if (currLevel == 4) {
			setupLevel4();
		}
	}

	void Update () {

		if (levelCompleted && Input.GetKeyDown(KeyCode.F3)) {
			nextLevel();
		}

		codeStr = input.text;
		UpdateObjects();

		
		if (codeStr != lastCodeStr) {
			clearCreatedObjects();

			source = engine.CreateScriptSourceFromFile("Assets/PythonPuzzles/Puzzle" + currLevel + ".py"); 
			source.Execute(scope);
			source = engine.CreateScriptSourceFromString(codeStr);

			try {
				source.Execute(scope);
				errors.text = "";
			}
			catch(Exception e) { // display error message
				print (e.Message);
				errors.text = "Error: " + e.Message;
			}

			if (checkPuzzleComplete()) {
				levelCompleted = true;
				displayLevelCompleted();
			}
		}
		lastCodeStr = codeStr;
	}
}
