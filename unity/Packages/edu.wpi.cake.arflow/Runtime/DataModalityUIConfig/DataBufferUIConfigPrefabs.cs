using UnityEngine;
using TMPro;
using System;

namespace CakeLab.ARFlow.DataModalityUIConfig
{
    public struct DataBufferUIConfigPrefabs
    {
        public GameObject headerTextPrefab;
        public GameObject bodyTextPrefab;
        public GameObject textFieldPrefab;
        public GameObject dropDownPrefab;
        public GameObject togglePrefab;
        public DataBufferUIConfigPrefabs(GameObject headerTextPrefab, GameObject bodyTextPrefab, GameObject textFieldPrefab, GameObject dropDownPrefab, GameObject togglePrefab)
        {
            this.headerTextPrefab = headerTextPrefab;
            this.bodyTextPrefab = bodyTextPrefab;
            this.textFieldPrefab = textFieldPrefab;
            this.dropDownPrefab = dropDownPrefab;
            this.togglePrefab = togglePrefab;
        }
    }

}

