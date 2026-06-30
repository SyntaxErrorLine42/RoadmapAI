import { Component, ChangeDetectionStrategy, inject, signal, computed, model, input, output, effect } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CourseDetail,
  RoadmapModule,
  RoadmapExam,
  RoadmapLesson,
  LessonStatus,
  ExamAttemptDetail,
  ExamSubmissionResult
} from '../../core/models/course.model';
import { QuestionComponent, QuestionOption } from './question.component';

@Component({
  selector: 'app-exam-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Button, QuestionComponent],
  templateUrl: './exam-dialog.component.html'
})
export class ExamDialogComponent {
  private readonly courseService = inject(CourseService);
  private readonly toast = inject(ToastService);

  // Models
  visible = model<boolean>(false);
  selectedExam = model<{ exam: RoadmapExam; moduleId: string; moduleTitle: string } | null>(null);
  viewAttemptId = model<string | null>(null);

  // Inputs
  courseId = input.required<string>();
  modules = input.required<RoadmapModule[]>();
  roadmapStats = input.required<any>();

  // Outputs
  courseUpdated = output<CourseDetail>();
  examAttemptsUpdated = output<void>();

  // Dialog State Signals
  examDialogHeader = signal('Module Exam');
  examReadOnly = signal(false);
  examReadOnlyHint = signal<string | null>(null);
  examError = signal<string | null>(null);
  examQuestions = signal<any[]>([]);
  examSelections = signal<Record<string, QuestionOption>>({});
  submittingExam = signal(false);

  // Results State Signals
  examResultDialogOpen = signal(false);
  examResult = signal<ExamSubmissionResult | null>(null);
  lastExamContext = signal<{ exam: RoadmapExam; moduleId: string; moduleTitle: string } | null>(null);

  // Computed exams helper to resolve module references during review
  readonly allExams = computed(() => {
    const modules = this.modules();
    return modules
      .slice()
      .sort((a, b) => a.orderIndex - b.orderIndex)
      .flatMap((module) =>
        module.exams
          .slice()
          .sort((a, b) => {
            if (a.isFinal !== b.isFinal) return a.isFinal ? 1 : -1;
            return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
          })
          .map((exam) => ({
            exam,
            module,
          }))
      );
  });

  // Recommendation text based on results
  readonly examRecommendation = computed(() => {
    const result = this.examResult();
    if (!result) return '';
    if (result.passed) {
      return 'Congratulations! You have successfully passed this exam.';
    }
    return 'You did not pass this exam. We recommend repeating the lessons listed below before retrying.';
  });

  // Final Exam completed message
  readonly finalExamPrompt = computed(() => {
    const result = this.examResult();
    if (!result) return '';
    const context = this.lastExamContext();
    if (context?.exam.isFinal && result.passed) {
      return 'Awesome work! You have fully completed the Course Final Exam!';
    }
    return '';
  });

  constructor() {
    // Effect to react when a fresh exam is selected for taking/viewing
    effect(() => {
      const selected = this.selectedExam();
      if (!selected || !this.visible()) return;

      // CRITICAL: If we are in review mode, do NOT trigger the fresh-take logic
      if (this.viewAttemptId()) return;

      this.examDialogHeader.set('Module Exam');
      this.examReadOnly.set(selected.exam.status === 'Completed');
      this.examReadOnlyHint.set(selected.exam.status === 'Completed' ? 'This exam is completed. Answers are read-only.' : null);
      this.examError.set(null);
      this.examQuestions.set([]);
      this.examSelections.set({});

      this.courseService.getExamQuestions(this.courseId(), selected.exam.id).subscribe({
        next: (questions) => this.examQuestions.set(questions),
        error: (err) => this.examError.set(err?.error?.message ?? 'Failed to load exam questions.'),
      });
    }, { allowSignalWrites: true });

    // Effect to react when an attempt ID is set for review
    effect(() => {
      const attemptId = this.viewAttemptId();
      if (!attemptId || !this.visible()) return;

      // Make sure fresh take mode is reset
      this.selectedExam.set(null);

      this.examDialogHeader.set('Exam Review');
      this.examReadOnly.set(true);
      this.examReadOnlyHint.set('Review mode: submitted answers are read-only.');
      this.examError.set(null);
      this.examQuestions.set([]);
      this.examSelections.set({});

      this.courseService.getExamAttemptDetail(this.courseId(), attemptId).subscribe({
        next: (attempt: ExamAttemptDetail) => {
          const match = this.allExams().find((entry) => entry.exam.id === attempt.examId);
          if (match) {
            this.selectedExam.set({
              exam: match.exam,
              moduleId: match.module.id,
              moduleTitle: match.module.title,
            });
          } else {
            this.selectedExam.set({
              exam: {
                id: attempt.examId,
                title: attempt.examTitle,
                isFinal: attempt.isFinal,
                status: 'Completed',
                questionCount: attempt.questions.length,
              },
              moduleId: '',
              moduleTitle: attempt.moduleTitle,
            });
          }

          this.examQuestions.set(
            attempt.questions.map((q) => ({
              id: q.questionId,
              orderIndex: q.orderIndex,
              questionText: q.questionText,
              optionA: q.optionA,
              optionB: q.optionB,
              optionC: q.optionC,
              optionD: q.optionD,
              isAiGenerated: q.isAiGenerated,
            }))
          );

          this.examSelections.set(
            attempt.questions.reduce<Record<string, QuestionOption>>((acc, q) => {
              acc[q.questionId] = q.selectedOption as QuestionOption;
              return acc;
            }, {})
          );
        },
        error: (err) => {
          this.examError.set(err?.error?.message ?? 'Failed to load exam attempt details.');
        },
      });
    }, { allowSignalWrites: true });
  }

  onExamDialogVisibleChange(visible: boolean): void {
    this.visible.set(visible);
    if (visible) return;

    this.selectedExam.set(null);
    this.viewAttemptId.set(null);
    this.examError.set(null);
    this.submittingExam.set(false);
    this.examDialogHeader.set('Module Exam');
    this.examReadOnly.set(false);
    this.examReadOnlyHint.set(null);
    this.examQuestions.set([]);
    this.examSelections.set({});
  }

  onExamResultVisibleChange(visible: boolean): void {
    this.examResultDialogOpen.set(visible);
    if (visible) return;
    this.examResult.set(null);
    this.lastExamContext.set(null);
  }

  selectExamOption(questionId: string, option: QuestionOption): void {
    if (this.examReadOnly()) return;

    this.examSelections.update((current) => ({
      ...current,
      [questionId]: option,
    }));
  }

  submitSelectedExam(): void {
    const selected = this.selectedExam();
    if (!selected) return;

    this.submittingExam.set(true);
    this.examError.set(null);

    const answers = this.examQuestions().map((question) => ({
      questionId: question.id,
      selectedOption: this.examSelections()[question.id] ?? '',
    }));

    this.courseService.submitExam(this.courseId(), selected.exam.id, { answers }).subscribe({
      next: (result) => {
        this.lastExamContext.set(selected);
        this.examResult.set(result);
        this.visible.set(false);
        this.examResultDialogOpen.set(true);
        this.submittingExam.set(false);
        this.selectedExam.set(null);

        this.courseService.getCourse(this.courseId()).subscribe({
          next: (updatedCourse) => {
            this.courseUpdated.emit(updatedCourse);
            this.examAttemptsUpdated.emit();

            if (!result.passed) return;

            this.toast.success('Exam completed', `You passed with ${result.scorePercent}%.`);

            const completedModule = updatedCourse.roadmap?.modules.find((m) => m.id === selected.moduleId);
            if (completedModule) {
              const allLessonsCompleted = completedModule.lessons.every((l) => l.status === LessonStatus.Completed);
              const allModuleExamsCompleted = completedModule.exams
                .filter((e) => !e.isFinal)
                .every((e) => e.status === 'Completed');

              if (allLessonsCompleted && allModuleExamsCompleted) {
                this.toast.success('Module completed', `${completedModule.title} is completed.`);
              }
            }

            if (this.roadmapStats().completionPercent >= 100) {
              this.toast.success('Course completed', `${updatedCourse.name} is now 100% complete.`);
            }
          },
        });
      },
      error: (err) => {
        this.submittingExam.set(false);
        const message = err?.error?.message ?? 'Failed to submit exam.';
        this.examError.set(message);
      },
    });
  }
}
