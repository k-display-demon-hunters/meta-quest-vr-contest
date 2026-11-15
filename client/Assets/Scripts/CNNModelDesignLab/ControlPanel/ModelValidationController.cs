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
        [SerializeField] private Color _readyColor = new Color32(64, 214, 176, 255);
        [SerializeField] private Color _problemColor = new Color32(242, 201, 76, 255);
        [SerializeField] private Color _placingColor = new Color32(170, 185, 192, 255);

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
            _issueCount.text = showProblem ? "모델 구성 확인 필요"
                : _isDragging ? "배치 중" : _hasValidationResult ? "모델 구성 확인 완료" : "학습 시작을 눌러 구성을 확인하세요";
            _statusAccent.color = showProblem ? _problemColor : _hasValidationResult ? _readyColor : _placingColor;
            if (!showProblem) { return; }

            ValidationIssue issue = _issues[_issueIndex];
            _location.text = "위치 · " + (issue.SlotIndex == null && issue.Message == "Spatial layers require Flatten before Dense or Output."
                ? "OUTPUT 앞의 연결" : GetIssueLocation(GetIssueLayer(issue)));
            DescribeIssue(issue.Message, out string title, out string reason, out string action);
            _title.text = title;
            _reason.text = "이유 · " + reason;
            _action.text = "이렇게 해보세요 · " + action + "\n수정 후 [학습 시작]을 다시 눌러 주세요.";
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
                    reason = "이 체험의 DENSE·OUTPUT은 가로·세로로 놓인 숫자를 한 줄로 펼쳐서 입력받아요.";
                    action = "공급 책상의 FLATTEN을 가져와 DENSE·OUTPUT 앞의 빈 슬롯에 놓으세요.";
                    break;
                case "Flatten requires spatial input.":
                    title = "이미 펼쳐진 입력이에요";
                    reason = "앞의 블록에서 이미 숫자를 한 줄로 펼쳤어요. 이 체험에서는 FLATTEN을 한 번만 사용해요.";
                    action = "이 FLATTEN 앞의 [설정 변경]을 누르고 삭제하세요.";
                    break;
                case "Spatial layers cannot follow Dense or Flatten.":
                    title = "Conv·Pooling의 위치를 바꾸세요";
                    reason = "CONV·POOLING은 가로·세로 형태가 필요해요. FLATTEN 뒤에서는 숫자가 한 줄로 펼쳐져 있어요.";
                    action = "이 CONV·POOLING을 잡아 첫 FLATTEN 앞의 빈 슬롯으로 옮기세요.";
                    break;
                case "Invalid Conv2D shape or placement.":
                    title = "커널이 입력보다 커요";
                    reason = "커널은 한 번에 살펴보는 영역이에요. 지금은 입력의 높이나 너비보다 커요.";
                    action = "[설정 변경]에서 [커널 높이]·[커널 너비]를 입력 크기 이하로 줄이세요.";
                    break;
                case "Invalid MaxPooling2D shape or placement.":
                    title = "풀링 영역이 입력보다 커요";
                    reason = "풀링 영역은 최댓값을 고르는 범위예요. 지금은 입력의 높이나 너비보다 커요.";
                    action = "[설정 변경]에서 [풀링 높이]·[풀링 너비]를 입력 크기 이하로 줄이세요.";
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
