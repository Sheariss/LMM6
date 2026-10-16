using System;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class TutorialManager
    {
        private readonly AssistantCatalog catalog;
        private readonly AssistantSaveData state;
        private readonly AssistantEngine engine;

        public TutorialManager(
            AssistantCatalog catalog,
            AssistantSaveData state,
            AssistantEngine engine)
        {
            this.catalog = catalog;
            this.state = state;
            this.engine = engine;
        }

        public AssistantTutorialState GetState(string tutorialId)
        {
            return state.tutorials.Find(
                item => item.id == tutorialId);
        }

        public bool CanStart(string tutorialId)
        {
            var definition = catalog.FindTutorial(tutorialId);

            if (definition == null)
                return false;

            var runtime = GetState(tutorialId);

            return
                (runtime == null ||
                 runtime.status == AssistantTutorialStatus.NotStarted) &&
                engine.Meets(definition.prerequisites, state, 0);
        }

        public bool Begin(string tutorialId)
        {
            if (!CanStart(tutorialId))
                return false;

            var definition = catalog.FindTutorial(tutorialId);
            var conversation = catalog.FindConversation(
                definition.conversationId);

            var runtime = GetState(tutorialId);

            if (runtime == null)
            {
                runtime = new AssistantTutorialState
                {
                    id = tutorialId
                };

                state.tutorials.Add(runtime);
            }

            runtime.status = AssistantTutorialStatus.Active;
            runtime.activeNodeId = conversation.entryNodeId;
            runtime.nodes.Clear();

            foreach (var node in conversation.nodes)
            {
                runtime.nodes.Add(new AssistantTutorialNodeState
                {
                    nodeId = node.id,
                    status = node.id == conversation.entryNodeId
                        ? AssistantTutorialNodeStatus.Active
                        : AssistantTutorialNodeStatus.Locked
                });
            }

            return true;
        }

        public bool CanAdvance(
            string conversationId,
            string fromNodeId)
        {
            var definition =
                catalog.FindTutorialForConversation(conversationId);

            if (definition == null)
                return true;

            var runtime = GetState(definition.id);

            return runtime != null &&
                   runtime.status == AssistantTutorialStatus.Active &&
                   runtime.activeNodeId == fromNodeId;
        }

        public void RecordTransition(
            string conversationId,
            string fromNodeId,
            string nextNodeId)
        {
            var definition =
                catalog.FindTutorialForConversation(conversationId);

            if (definition == null)
                return;

            var runtime = GetState(definition.id);

            if (runtime == null)
                throw new InvalidOperationException(
                    "Tutorial must begin before advancing.");

            SetNodeStatus(
                runtime,
                fromNodeId,
                AssistantTutorialNodeStatus.Completed);

            runtime.activeNodeId = nextNodeId;

            if (string.IsNullOrWhiteSpace(nextNodeId))
            {
                runtime.status = AssistantTutorialStatus.Completed;
                return;
            }

            SetNodeStatus(
                runtime,
                nextNodeId,
                AssistantTutorialNodeStatus.Active);
        }

        public void Skip(string tutorialId)
        {
            var definition = catalog.FindTutorial(tutorialId);

            if (definition == null)
                return;

            var runtime = GetState(tutorialId);

            if (runtime == null)
            {
                runtime = new AssistantTutorialState
                {
                    id = tutorialId
                };

                state.tutorials.Add(runtime);
            }

            if (runtime.status == AssistantTutorialStatus.Completed)
                return;

            runtime.status = AssistantTutorialStatus.Skipped;
            runtime.activeNodeId = null;

            foreach (var node in runtime.nodes)
            {
                if (node.status != AssistantTutorialNodeStatus.Completed)
                    node.status = AssistantTutorialNodeStatus.Skipped;
            }
        }

        public void ResetForReplay(string tutorialId)
        {
            // Investigation facts and lesson progress remain intact.
            state.tutorials.RemoveAll(item => item.id == tutorialId);
        }

        private static void SetNodeStatus(
            AssistantTutorialState runtime,
            string nodeId,
            AssistantTutorialNodeStatus status)
        {
            var node = runtime.nodes.Find(
                item => item.nodeId == nodeId);

            if (node == null)
            {
                node = new AssistantTutorialNodeState
                {
                    nodeId = nodeId
                };

                runtime.nodes.Add(node);
            }

            node.status = status;
        }
    }
}