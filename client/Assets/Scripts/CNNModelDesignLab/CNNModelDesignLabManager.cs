using System.Collections.Generic;
using ModelDesign;
using UnityEngine;

namespace ModelDesignScene
{
    public sealed class CNNModelDesignLabManager : MonoBehaviour
    {
        [SerializeField] private ModelDesignSceneController _sceneController;
        [SerializeField] private ModelConveyorController _conveyor;

        private readonly List<LayerDefinition> _connectedLayers = new List<LayerDefinition>();
        private ModelWorkspace _workspace;

        private void Awake()
        {
            _workspace = new ModelWorkspace();
        }

        private void Start()
        {
            // All scene blocks have completed Awake before their model data is assigned.
            _sceneController.Initialize(this, _workspace);
            _conveyor.Initialize(_sceneController.InputBlock, _sceneController.OutputBlock, _sceneController);
            RefreshModelOrder();
        }

        public void AddBlock(ModelBlockView block)
        {
            _workspace.AddLayer(block.Layer);
        }

        public void BeginBlockMove(ModelBlockView block)
        {
            if (_conveyor.RemoveBlock(block))
            {
                RefreshModelOrder();
            }
        }

        public void PreviewBlockPlacement(ModelBlockView block)
        {
            if (_conveyor != null)
            {
                _conveyor.PreviewPlacement(block);
            }
        }

        public void FinishBlockMove(ModelBlockView block, bool cancelled)
        {
            _conveyor.ClearPreview();
            if (!cancelled && _conveyor.TryPlaceBlock(block))
            {
                RefreshModelOrder();
            }
        }

        public void DeleteBlock(ModelBlockView block)
        {
            // Disconnect the old sequence before removing its layer from the workspace.
            BeginBlockMove(block);
            _workspace.RemoveLayer(block.Layer);
        }

        private void RefreshModelOrder()
        {
            for (int index = 1; index < _connectedLayers.Count; index++)
            {
                _workspace.Disconnect(_connectedLayers[index - 1], _connectedLayers[index]);
            }

            _connectedLayers.Clear();
            _connectedLayers.Add(_workspace.Input);
            for (int index = 0; index < _conveyor.SlotCount; index++)
            {
                ModelBlockView block = _conveyor.GetBlock(index);
                if (block != null)
                {
                    _connectedLayers.Add(block.Layer);
                }
            }
            _connectedLayers.Add(_workspace.Output);

            // Empty slots do not create layers; physical block positions stay unchanged.
            for (int index = 1; index < _connectedLayers.Count; index++)
            {
                _workspace.Connect(_connectedLayers[index - 1], _connectedLayers[index]);
            }
        }

        private void OnDestroy()
        {
            _workspace?.Dispose();
        }
    }
}
