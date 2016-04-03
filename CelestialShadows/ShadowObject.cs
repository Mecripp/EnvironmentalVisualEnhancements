﻿using EVEManager;
using ShaderLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Utils;

namespace CelestialShadows
{

    public class ShadowMaterial : MaterialManager
    {
    }

    public class ShadowComponent : MonoBehaviour
    {
        Material shadowMat;
        CelestialBody body;
        List<CelestialBody> shadowList;

        internal void Apply(Material mat, CelestialBody cb, List<CelestialBody> list)
        {
            shadowMat = mat;
            body = cb;
            shadowList = list;
        }

        internal void OnPreCull()
        {
            
            Matrix4x4 bodies = new Matrix4x4();
            int i = 0;
            foreach (CelestialBody cb in shadowList)
            {
                bodies.SetRow(i, cb.scaledBody.transform.position);
                bodies[i, 3] = (float)(ScaledSpace.InverseScaleFactor * cb.Radius);
                i++;
            }
            if (shadowMat != null)
            {
                shadowMat.SetVector("_SunPos", Sun.Instance.sun.scaledBody.transform.position);
                shadowMat.SetMatrix("_ShadowBodies", bodies);
            }

            foreach (Transform child in body.scaledBody.transform)
            {
                MeshRenderer cmr = child.GetComponent<MeshRenderer>();
                if (cmr != null)
                {
                    cmr.material.SetVector("_SunPos", Sun.Instance.sun.scaledBody.transform.position);
                    cmr.material.SetMatrix("_ShadowBodies", bodies);
                }
            }
            /*
            ShadowManager.Log("_SunPos: " + Sun.Instance.sun.scaledBody.transform.position);
            ShadowManager.Log("_SunRadius: " + (float)(ScaledSpace.InverseScaleFactor * Sun.Instance.sun.Radius));
            ShadowManager.Log("Mun: " + bodies.ToString());
            */
        }
    }

    public class ShadowObject : IEVEObject
    {
#pragma warning disable 0649
        [ConfigItem, GUIHidden]
        private String body;
        /* [ConfigItem]
         ShadowMaterial shadowMaterial = null;
         */
        [ConfigItem]
        List<String> caster = null;
        [ConfigItem]
        bool hasSurface = true;

        String materialName = Guid.NewGuid().ToString();
        Material shadowMat;

        private static Shader shadowShader;
        private static Shader ShadowShader
        {
            get
            {
                if (shadowShader == null)
                {
                    shadowShader = ShaderLoaderClass.FindShader("EVE/PlanetLight");
                }
                return shadowShader;
            }
        }


        public void LoadConfigNode(ConfigNode node)
        {
            ConfigHelper.LoadObjectFromConfig(this, node);
        }

        public void Apply()
        {
            ShadowManager.Log("Applying to " + body);
            CelestialBody celestialBody = Tools.GetCelestialBody(body);
            
            Transform transform = Tools.GetScaledTransform(body);
            if (transform != null )
            {
                MeshRenderer mr = transform.GetComponent<MeshRenderer>();
                if (mr != null && hasSurface)
                {
                    shadowMat = new Material(ShadowShader);

                    //shadowMaterial.ApplyMaterialProperties(shadowMat);
                    shadowMat.SetFloat("_SunRadius", (float)(ScaledSpace.InverseScaleFactor * Sun.Instance.sun.Radius));
                    shadowMat.name = materialName;
                    List<Material> materials = new List<Material>(mr.materials);
                    materials.Add(shadowMat);
                    mr.materials = materials.ToArray();
                }

                foreach (Transform child in celestialBody.scaledBody.transform)
                {
                    MeshRenderer cmr = child.GetComponent<MeshRenderer>();
                    if (cmr != null)
                    {
                        cmr.material.SetFloat("_SunRadius", (float)(ScaledSpace.InverseScaleFactor * Sun.Instance.sun.Radius));
                    }
                }
                ShadowComponent sc = ScaledCamera.Instance.galaxyCamera.gameObject.AddComponent<ShadowComponent>();
                sc.name = materialName;

                List<CelestialBody> casters = new List<CelestialBody>();
                if (caster != null)
                {
                    foreach (String b in caster)
                    {
                        casters.Add(Tools.GetCelestialBody(b));
                    }
                }
                sc.Apply(shadowMat, celestialBody, casters);
            }
           
        }

        

        public void Remove()
        {
            CelestialBody celestialBody = Tools.GetCelestialBody(body);
            ShadowManager.Log("Removing Shadow obj");
            Transform transform = Tools.GetScaledTransform(body);
            if (transform != null)
            {
                MeshRenderer mr = transform.GetComponent<MeshRenderer>();
                if (mr != null && hasSurface)
                {
                    List<Material> materials = new List<Material>(mr.materials);
                    materials.Remove(materials.Find(mat => mat.name.Contains(materialName)));
                    mr.materials = materials.ToArray();
                }
                GameObject.DestroyImmediate(ScaledCamera.Instance.galaxyCamera.gameObject.GetComponents<ShadowComponent>().First(sc => sc.name == materialName));
            }
        }
    }
}
