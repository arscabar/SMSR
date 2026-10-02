namespace SMSR.App.Mvp;
public sealed record GraphVocabularyTerm(string Term, int NodeFrequency);
public sealed record GraphVocabulary(int Revision, GraphVocabularyTerm[] Terms, int Total, bool Truncated);
public sealed record GraphQuestionRequest(string ProjectId, string Question, string[]? Terms = null,
    int MaxTokens = 4000, string Direction = "both", int Depth = 2, bool IncludeInferred = false, string? Relation = null);
public sealed record GraphQuestionHit(GraphNode Node, double Score, string[] MatchedTerms, bool Ambiguous);
public sealed record GraphQuestionSearch(int Revision, string[] Terms, GraphQuestionHit[] Hits, bool Truncated, string Limitation);
public sealed record GraphFeedbackLesson(GraphFeedback Feedback, int Count, string Guidance);
public sealed record GraphFeedbackLessons(int Revision, GraphFeedbackLesson[] Items, int StaleExcluded, bool Truncated);
public sealed record GraphQuestionEvidence(int Revision, GraphNode[] Nodes, GraphEdge[] Edges,
    GraphFeedbackLesson[] Lessons, bool Truncated, int ByteCount, int Budget, string BudgetPolicy,
    string Limitation);
