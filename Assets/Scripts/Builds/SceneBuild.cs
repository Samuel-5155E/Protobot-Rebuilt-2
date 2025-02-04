﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Protobot.Builds {
    public static class SceneBuild {
        public static Action<BuildData> OnGenerateBuild;

        public static BuildData DefaultBuild => new BuildData {
            name = "DefaultBuild",
            camera = new CameraData {
                xPos = 0,
                yPos = 0,
                zPos = 0,
                xRot = 30,
                yRot = 45,
                zRot = 0,
                zoom = -15,
                isOrtho = false
            },
            lastWriteTime = DateTime.Now.ToString("MMMM dd, yyyy h:mm tt")
        };

        /// <summary>
        /// Generates all the objects into the scene using given BuildData
        /// </summary>
        public static void GenerateBuild(BuildData buildData) {
            Debug.Log("Generating a build with " +
                      (buildData.parts == null ? 0 : buildData.parts.Length) + " parts");

            PartsManager.DestroyLoadedObjects();

            //Camera Data
            CameraData camData = buildData.camera;

            Vector3 savedCamPos = new Vector3((float) camData.xPos, (float) camData.yPos, (float) camData.zPos);
            Vector3 savedCamAngle = new Vector3((float) camData.xRot, (float) camData.yRot, (float) camData.zRot);

            PivotCamera.main.SetTransform(savedCamPos, savedCamAngle, (float) camData.zoom);

            var projectionSwitcher = PivotCamera.main.GetComponent<ProjectionSwitcher>();

            if (camData.isOrtho)
                projectionSwitcher.SwitchToOrtho(0);
            else
                projectionSwitcher.SwitchToPers(0);

            //Parts
            if (buildData.parts != null) {
                List<ObjectData> connectingObjects = new();

                foreach (ObjectData part in buildData.parts) {
                    if (PartsManager.GetPartType(part.partId).connectingPart) {
                        connectingObjects.Add(part);
                    }
                    else
                        GenerateObject(part);
                }

                foreach (ObjectData part in connectingObjects)
                    GenerateObject(part);

            }
            
            OnGenerateBuild?.Invoke(buildData);
        }

        private static GameObject GenerateObject(ObjectData objectData) => 
            PartsManager.GeneratePart(objectData.partId, objectData.GetPos(), objectData.GetRot());


        /// <summary>
        /// Generates an empty build with default camera data into the scene
        /// </summary>
        public static BuildData GenerateDefault(string buildName) {
            var newBuild = DefaultBuild;
            newBuild.name = buildName;

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            newBuild.fileName = new string(buildName.Where(m => !invalidChars.Contains(m)).ToArray<char>());

            GenerateBuild(newBuild);

            return newBuild;
        }

        /// <summary>
        /// Converts scene objects into BuildData
        /// </summary>
        /// <remarks>Does not contain lastWriteTime, createTime, fileName, or name</remarks>
        public static BuildData ToBuildData() {
            //Camera
            PivotCamera cam = PivotCamera.main;
            ProjectionSwitcher projectionSwitcher = cam.GetComponent<ProjectionSwitcher>();

            CameraData newCameraData = new CameraData {
                xPos = cam.focusPosition.x,
                yPos = cam.focusPosition.y,
                zPos = cam.focusPosition.z,

                xRot = cam.lookAngle.x,
                yRot = cam.lookAngle.y,
                zRot = cam.lookAngle.z,

                zoom = cam.focusDistance,

                isOrtho = projectionSwitcher.isOrtho
            };

            //Parts
            List<GameObject> sceneObjs = PartsManager.FindLoadedObjects();

            ObjectData[] newParts = new ObjectData[sceneObjs.Count];

            for (int i = 0; i < newParts.Length; i++) {
                Transform tForm = sceneObjs[i].transform;
                SavedObject savedData = tForm.GetComponent<SavedObject>();

                var position = tForm.position;
                var eulerAngles = tForm.eulerAngles;
                newParts[i] = new ObjectData {
                    partId = savedData.id,
                    states = savedData.state,

                    xPos = position.x,
                    yPos = position.y,
                    zPos = position.z,

                    xRot = eulerAngles.x,
                    yRot = eulerAngles.y,
                    zRot = eulerAngles.z,
                };
            }


            return new BuildData {
                camera = newCameraData,
                parts = newParts,
            };
        }
    }
}