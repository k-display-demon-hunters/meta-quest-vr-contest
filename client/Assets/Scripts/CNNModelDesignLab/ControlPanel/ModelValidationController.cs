using System;
using System.Collections.Generic;
using ModelDesignScene.Model;
using ModelDesignScene.Execution;
using UnityEngine;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelValidationController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _reason;
        [SerializeField] private Text _action;
        [SerializeField] private Text _location;
        [SerializeField] private Text _issueCount;
        [SerializeField] private Button _previousIssue;
        [SerializeField] private Button _nextIssue;
        [SerializeField] private Image _statusAccent;
        [SerializeField] private Text[] _slotLabels;
        [SerializeField] private Image[] _slotSurfaces;
        [SerializeField] private Color _readyColor = new Color(0.22f, 0.8f, 0.59f);
        [SerializeField] private Color _problemColor = new Color(1f, 0.68f, 0.22f);
        [SerializeField] private Color _placingColor = new Color(0.28f, 0.71f, 0.95f);
        [SerializeField] private Color _emptySlotColor = new Color(0.08f, 0.12f, 0.17f);
        [SerializeField] private Color _occupiedSlotColor = new Color(0.14f, 0.3f, 0.34f);

        private IReadOnlyList<ValidationIssue> _issues = Array.Empty<ValidationIssue>();
        private IReadOnlyList<LayerDefinition> _slotLayers = Array.Empty<LayerDefinition>();
        private int _issueIndex;
        private bool _isDragging;
        private bool _hasValidationResult;

        private void Awake()
        {
            _previousIssue.onClick.AddListener(HandlePreviousIssue);
            _nextIssue.onClick.AddListener(HandleNextIssue);
        }

        internal void ShowValidation(IReadOnlyList<ValidationIssue> issues,
            IReadOnlyList<LayerDefinition> slotLayers, bool isDragging, bool hasValidationResult)
        {
            ValidationIssue previous = _issues.Count > 0 ? _issues[_issueIndex] : null;
            _issues = issues ?? Array.Empty<ValidationIssue>();
            _slotLayers = slotLayers ?? Array.Empty<LayerDefinition>();
            _isDragging = isDragging;
            _hasValidationResult = hasValidationResult;
            _issueIndex = 0;
            if (previous != null)
            {
                for (int index = 0; index < _issues.Count; index++)
                {
                    ValidationIssue candidate = _issues[index];
                    if (candidate.Message == previous.Message && candidate.SlotIndex == previous.SlotIndex)
                    {
                        _issueIndex = index;
                        break;
                    }
                }
            }
            RefreshView();
        }

        private void RefreshView()
        {
            bool showProblem = !_isDragging && _issues.Count > 0;
            ValidationIssue issue = showProblem ? _issues[_issueIndex] : null;
            LayerDefinition problemLayer = GetIssueLayer(issue);
            int occupiedCount = 0;
            for (int index = 0; index < _slotLabels.Length; index++)
            {
                LayerDefinition layer = index < _slotLayers.Count ? _slotLayers[index] : null;
                if (layer != null)
                {
                    occupiedCount++;
                }
                _slotLabels[index].text = $"{index + 1:00}\n" + (layer == null ? "빈 슬롯" : GetLayerName(layer.Type));
                _slotSurfaces[index].color = layer != null && ReferenceEquals(layer, problemLayer)
                    ? _problemColor : layer == null ? _emptySlotColor : _occupiedSlotColor;
            }

            _previousIssue.interactable = showProblem && _issueIndex > 0;
            _nextIssue.interactable = showProblem && _issueIndex + 1 < _issues.Count;
            _issueCount.text = showProblem ? $"문제 {_issueIndex + 1} / {_issues.Count}"
                : _isDragging ? "배치 중" : _hasValidationResult ? "검사 완료" : "검사 대기";
            if (_isDragging)
            {
                _statusAccent.color = _placingColor;
                _title.text = "블록을 배치하고 있어요";
                _location.text = "빈 슬롯에 놓기";
                _reason.text = "잡은 블록을 컨베이어의 빈 슬롯 위로 옮겨 주세요.";
                _action.text = "Grip을 놓으면 슬롯에 붙습니다. 슬롯 밖이나 이미 찬 슬롯에 놓으면 블록이 삭제됩니다.";
                return;
            }
            if (!showProblem)
            {
                if (occupiedCount == 0)
                {
                    _statusAccent.color = _placingColor;
                    _title.text = "블록으로 모델을 만들어 보세요";
                    _location.text = "오른쪽 부품 선반 → 컨베이어 빈 슬롯";
                    _issueCount.text = "처음 시작하기";
                    _reason.text = "1. 오른쪽 선반의 블록을 Grip으로 잡으세요.\n2. 컨베이어의 빈 슬롯 위에서 Grip을 놓으세요.";
                    _action.text = "슬롯의 설정 버튼을 Trigger로 누르세요.\n예시: Conv → Pooling → Flatten → Dense → 학습 시작";
                    return;
                }
                if (!_hasValidationResult)
                {
                    _statusAccent.color = _placingColor;
                    _title.text = "학습 시작 시 모델을 검사합니다";
                    _location.text = $"INPUT → {occupiedCount}개 블록 → OUTPUT";
                    _reason.text = "원하는 블록과 설정값을 배치하세요. 아직 전체 모델 검사를 진행하지 않았습니다.";
                    _action.text = "학습 시작을 누르면 순서·입력 크기·자원 한도를 검사한 뒤 학습합니다.";
                    return;
                }
                _statusAccent.color = _readyColor;
                _title.text = "블록 순서 확인 완료";
                _location.text = $"INPUT → {occupiedCount}개 블록 → OUTPUT";
                _reason.text = "블록 순서와 모델 크기 검사를 통과했습니다. 학습 결과는 학습·검증 지표에서 확인하세요.";
                _action.text = "학습 시작을 누르세요. Conv·Pooling 뒤에 Dense·Output을 둘 때는 Flatten이 필요합니다.";
                return;
            }

            _statusAccent.color = _problemColor;
            _location.text = GetIssueLocation(problemLayer);
            DescribeIssue(issue.Message, out string title, out string reason, out string action);
            _title.text = title;
            _reason.text = reason;
            _action.text = action;
        }

        private string GetIssueLocation(LayerDefinition layer)
        {
            if (layer == null)
            {
                return "전체 모델 연결";
            }
            for (int index = 0; index < _slotLayers.Count; index++)
            {
                if (ReferenceEquals(_slotLayers[index], layer))
                {
                    return $"슬롯 {index + 1} · {GetLayerName(layer.Type)}";
                }
            }
            return layer.Type == LayerType.Input || layer.Type == LayerType.Output
                ? "고정 " + GetLayerName(layer.Type) : GetLayerName(layer.Type) + " 블록";
        }

        private LayerDefinition GetIssueLayer(ValidationIssue issue)
        {
            return issue?.SlotIndex is int slot ? _slotLayers[slot] : null;
        }

        private static string GetLayerName(LayerType type)
        {
            switch (type)
            {
                case LayerType.Conv2D: return "CONV";
                case LayerType.MaxPooling2D: return "POOLING";
                case LayerType.Flatten: return "FLATTEN";
                case LayerType.Dense: return "DENSE";
                case LayerType.Input: return "INPUT";
                case LayerType.Output: return "OUTPUT";
                default: return type.ToString();
            }
        }

        private static void DescribeIssue(string message, out string title, out string reason, out string action)
        {
            switch (message)
            {
                case "Spatial layers require Flatten before Dense or Output.":
                    title = "FLATTEN이 필요해요";
                    reason = "INPUT 이미지와 Conv·Pooling 결과는 공간 형태입니다. Dense·Output은 펼친 벡터를 입력받습니다.";
                    action = "이 Dense·Output 앞에 FLATTEN을 놓으세요.";
                    break;
                case "Flatten requires spatial input.":
                    title = "이미 펼쳐진 입력이에요";
                    reason = "앞의 Flatten·Dense가 이미 벡터를 출력하므로 다시 Flatten할 수 없습니다.";
                    action = "이 FLATTEN을 빼거나 Conv·Pooling 다음, Dense 앞에 놓으세요.";
                    break;
                case "Spatial layers cannot follow Dense or Flatten.":
                    title = "Conv·Pooling의 위치를 바꾸세요";
                    reason = "Flatten·Dense 뒤에는 이미지 형태가 남아 있지 않습니다.";
                    action = "이 Conv·Pooling을 첫 FLATTEN·DENSE 앞으로 옮기세요.";
                    break;
                default:
                    title = "모델 구성을 확인하세요";
                    reason = string.IsNullOrWhiteSpace(message) ? "모델 구성에 확인이 필요한 문제가 있습니다." : message;
                    action = "표시된 블록의 순서와 설정을 확인한 뒤 다시 학습을 시작하세요.";
                    break;
            }
        }

        private void HandlePreviousIssue()
        {
            if (!_isDragging && _issueIndex > 0)
            {
                _issueIndex--;
                RefreshView();
            }
        }

        private void HandleNextIssue()
        {
            if (!_isDragging && _issueIndex + 1 < _issues.Count)
            {
                _issueIndex++;
                RefreshView();
            }
        }

        private void OnDestroy()
        {
            if (_previousIssue != null)
            {
                _previousIssue.onClick.RemoveListener(HandlePreviousIssue);
            }
            if (_nextIssue != null)
            {
                _nextIssue.onClick.RemoveListener(HandleNextIssue);
            }
        }
    }
}
