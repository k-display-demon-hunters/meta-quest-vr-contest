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
        [SerializeField] private GameObject _diagnosticDetails;
        [SerializeField] private Text _reason;
        [SerializeField] private Text _action;
        [SerializeField] private Text _location;
        [SerializeField] private Text _issueCount;
        [SerializeField] private GameObject _diagnosticsTip;
        [SerializeField] private Button _previousIssue;
        [SerializeField] private Button _nextIssue;
        [SerializeField] private Image _statusAccent;
        [SerializeField] private Color _readyColor = new Color32(57, 230, 255, 255);
        [SerializeField] private Color _problemColor = new Color32(242, 201, 76, 255);
        [SerializeField] private Color _placingColor = new Color32(106, 139, 158, 255);

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
            bool showIssueNavigation = showProblem && _issues.Count > 1;
            _diagnosticDetails.SetActive(showProblem);
            _diagnosticsTip.SetActive(showIssueNavigation);
            _previousIssue.gameObject.SetActive(showIssueNavigation);
            _nextIssue.gameObject.SetActive(showIssueNavigation);
            _previousIssue.interactable = showIssueNavigation && _issueIndex > 0;
            _nextIssue.interactable = showIssueNavigation && _issueIndex + 1 < _issues.Count;
            _issueCount.text = showProblem ? $"문제 {_issueIndex + 1} / {_issues.Count}"
                : _isDragging ? "배치 중" : _hasValidationResult ? "블록 순서 확인 완료" : "실행 전 검사 대기";
            _statusAccent.color = showProblem ? _problemColor : _hasValidationResult ? _readyColor : _placingColor;
            if (!showProblem) { return; }

            ValidationIssue issue = _issues[_issueIndex];
            _location.text = GetIssueLocation(GetIssueLayer(issue));
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
