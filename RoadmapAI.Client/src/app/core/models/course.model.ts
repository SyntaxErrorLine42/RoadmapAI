export enum CourseStatus {
  Processing = 'Processing',
  Ready = 'Ready',
  RoadmapGenerating = 'RoadmapGenerating',
  RoadmapReady = 'RoadmapReady',
  Error = 'Error',
  Completed = 'Completed',
}

export enum MaterialStatus {
  Uploaded = 'Uploaded',
  Ingesting = 'Ingesting',
  Ready = 'Ready',
  Error = 'Error',
}

export enum LessonStatus {
  NotGenerated = 'NotGenerated',
  Generating = 'Generating',
  Generated = 'Generated',
  Error = 'Error',
  Completed = 'Completed',
}

export enum MaterialType {
  Lecture = 'Lecture',
  Exam = 'Exam',
  LabExercise = 'LabExercise',
  Other = 'Other',
}

export interface CourseSummary {
  id: string;
  name: string;
  status: CourseStatus;
  createdAt: string;
  materialCount: number;
  completionPercent: number;
}

export interface CourseDetail {
  id: string;
  name: string;
  summary: string | null;
  status: CourseStatus;
  createdAt: string;
  materials: CourseMaterial[];
  roadmap: CourseRoadmap | null;
}

export interface CourseMaterial {
  id: string;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  type: MaterialType;
  status: MaterialStatus;
  uploadedAt: string;
}

export interface RenameCoursePayload {
  name: string;
}

export interface CourseRoadmap {
  id: string;
  courseId: string;
  summaryMarkdown: string;
  generatedAt: string;
  modules: RoadmapModule[];
}

export interface RoadmapModule {
  id: string;
  orderIndex: number;
  title: string;
  isCompleted: boolean;
  lessons: RoadmapLesson[];
  exams: RoadmapExam[];
}

export interface RoadmapLesson {
  id: string;
  orderIndex: number;
  title: string;
  contentMarkdown: string;
  status: LessonStatus;
  generatedAt: string | null;
}

export interface RoadmapExam {
  id: string;
  title: string;
  isFinal: boolean;
  status: string;
  questionCount: number;
}

export interface RoadmapExamQuestion {
  id: string;
  orderIndex: number;
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  isAiGenerated: boolean;
}

export interface GenerateRoadmapResponse {
  courseId: string;
  status: CourseStatus;
  message: string;
}

export interface LessonGeneratedResponse {
  courseId: string;
  lessonId: string;
  status: LessonStatus;
  contentMarkdown: string;
  task?: LessonTask | null;
}

export interface LessonTask {
  lessonId: string;
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  helpMarkdown: string;
  completedCorrectOption?: string | null;
}

export interface SubmitLessonTaskRequest {
  selectedOption: string;
}

export interface SubmitLessonTaskResult {
  lessonId: string;
  isCorrect: boolean;
  completed: boolean;
  message: string;
  helpMarkdown: string;
}

export interface SubmitExamAnswer {
  questionId: string;
  selectedOption: string;
}

export interface SubmitExamRequest {
  answers: SubmitExamAnswer[];
}

export interface ExamSubmissionResult {
  examId: string;
  scorePercent: number;
  passed: boolean;
  correctAnswers: number;
  totalQuestions: number;
  moduleTitle: string;
  recommendationText: string;
  recommendedLessonTitles: string[];
}

export interface ExamAttemptSummary {
  id: string;
  examId: string;
  examTitle: string;
  moduleTitle: string;
  isFinal: boolean;
  scorePercent: number;
  passed: boolean;
  submittedAt: string;
}

export interface ExamAttemptQuestionReview {
  questionId: string;
  orderIndex: number;
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  isAiGenerated: boolean;
  selectedOption: string;
  correctOption: string;
  isCorrect: boolean;
}

export interface ExamAttemptDetail {
  id: string;
  examId: string;
  examTitle: string;
  moduleTitle: string;
  isFinal: boolean;
  scorePercent: number;
  passed: boolean;
  submittedAt: string;
  questions: ExamAttemptQuestionReview[];
}

export interface CourseReviewSummary {
  id: string;
  createdAt: string;
  preview: string;
}

export interface CourseReviewDetail {
  id: string;
  courseId: string;
  createdAt: string;
  contentMarkdown: string;
}
