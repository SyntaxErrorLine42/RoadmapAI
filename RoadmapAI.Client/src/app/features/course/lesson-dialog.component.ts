import { Component, ChangeDetectionStrategy, inject, signal, computed, model, input, output, effect } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import { marked } from 'marked';
import * as Prism from 'prismjs';
import 'prismjs/components/prism-c';
import 'prismjs/components/prism-python';
import 'prismjs/components/prism-java';
import 'prismjs/components/prism-bash';
import {
  CourseDetail,
  RoadmapLesson,
  LessonTask,
  LessonStatus,
  SubmitLessonTaskResult,
  RoadmapModule,
  RoadmapExam
} from '../../core/models/course.model';
import { QuestionComponent, QuestionOption } from './question.component';

@Component({
  selector: 'app-lesson-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Button, QuestionComponent],
  templateUrl: './lesson-dialog.component.html'
})
export class LessonDialogComponent {
  private readonly courseService = inject(CourseService);
  private readonly toast = inject(ToastService);

  readonly lessonStatus = LessonStatus;

  // Modals / Bindings
  visible = model<boolean>(false);
  selectedLesson = model<RoadmapLesson | null>(null);

  // Inputs
  courseId = input.required<string>();
  modules = input.required<RoadmapModule[]>();

  // Outputs
  courseUpdated = output<CourseDetail>();
  openExamEvent = output<{ exam: RoadmapExam; module: RoadmapModule }>();

  // Local dialog states
  lessonGenerating = signal(false);
  lessonGenerationError = signal<string | null>(null);
  lessonTask = signal<LessonTask | null>(null);
  lessonTaskSelection = signal<QuestionOption | null>(null);
  lessonTaskError = signal<string | null>(null);
  lessonTaskMessage = signal<string | null>(null);
  lessonTaskHelpVisible = signal(false);
  submittingLessonTask = signal(false);

  readonly lessonDialogHeader = computed(() => {
    const lesson = this.selectedLesson();
    if (!lesson) return 'Lesson';
    return `Lesson ${lesson.orderIndex}: ${lesson.title}`;
  });

  // Effects to trigger data loading when a lesson is selected
  constructor() {
    effect(() => {
      const lesson = this.selectedLesson();
      if (!lesson || !this.visible()) return;

      this.lessonGenerationError.set(null);
      this.lessonTaskSelection.set(null);
      this.lessonTaskError.set(null);
      this.lessonTaskMessage.set(null);
      this.lessonTaskHelpVisible.set(false);

      if (lesson.status === LessonStatus.Generated || lesson.status === LessonStatus.Completed) {
        this.loadLessonTask(lesson.id);
      } else {
        this.lessonGenerating.set(true);
        this.generateLesson(lesson.id);
      }
    }, { allowSignalWrites: true });

    // Automatically highlight code blocks when content or visibility changes
    effect(() => {
      const content = this.renderedContent();
      const help = this.renderedHelpHtml();
      const isVisible = this.visible();
      const isGenerating = this.lessonGenerating();

      if (isVisible && !isGenerating) {
        setTimeout(() => {
          Prism.highlightAll();
        }, 50);
      }
    });
  }

  // Render markdown content safely
  renderedContent = computed(() => {
    const raw = this.selectedLesson()?.contentMarkdown;
    return marked.parse(raw || 'Lesson content placeholder: once generated, this section will show detailed explanations and a practice task from your materials.') as string;
  });

  renderedHelpHtml = computed(() => {
    const raw = this.lessonTask()?.helpMarkdown;
    return marked.parse(raw || '') as string;
  });

  onVisibleChange(visible: boolean): void {
    this.visible.set(visible);
    if (visible) return;
    this.lessonGenerating.set(false);
    this.lessonGenerationError.set(null);
    this.selectedLesson.set(null);
    this.lessonTask.set(null);
    this.lessonTaskSelection.set(null);
    this.lessonTaskError.set(null);
    this.lessonTaskMessage.set(null);
    this.lessonTaskHelpVisible.set(false);
    this.submittingLessonTask.set(false);
  }

  private loadLessonTask(lessonId: string): void {
    this.lessonTask.set(null);
    this.lessonTaskError.set(null);

    this.courseService.getLessonTask(this.courseId(), lessonId).subscribe({
      next: (task) => {
        this.lessonTask.set(task);
        if (this.selectedLesson()?.status === LessonStatus.Completed && task.completedCorrectOption) {
          this.lessonTaskSelection.set(task.completedCorrectOption as QuestionOption);
          this.lessonTaskMessage.set('Correct answer. Lesson completed.');
        }
      },
      error: () => {
        this.lessonTaskError.set('Task is not available yet.');
      },
    });
  }

  selectLessonTaskOption(option: QuestionOption): void {
    if (this.selectedLesson()?.status === LessonStatus.Completed) return;
    this.lessonTaskSelection.set(option);
    this.lessonTaskError.set(null);
  }

  toggleLessonTaskHelp(): void {
    this.lessonTaskHelpVisible.update((value) => !value);
  }

  completeCurrentLesson(): void {
    this.submitLessonTask();
  }

  private submitLessonTask(): void {
    const lesson = this.selectedLesson();
    const selectedOption = this.lessonTaskSelection();
    if (!lesson) return;

    if (!selectedOption) {
      this.lessonTaskError.set('Select an option first.');
      return;
    }

    this.submittingLessonTask.set(true);
    this.lessonTaskError.set(null);
    this.lessonTaskMessage.set(null);

    this.courseService.submitLessonTask(this.courseId(), lesson.id, { selectedOption }).subscribe({
      next: (result: SubmitLessonTaskResult) => {
        this.submittingLessonTask.set(false);
        this.lessonTaskMessage.set(result.message);

        if (!result.isCorrect) {
          this.lessonTaskHelpVisible.set(true);
          return;
        }

        this.lessonTaskHelpVisible.set(false);
        this.selectedLesson.update((value) => (value ? { ...value, status: LessonStatus.Completed } : value));
        this.toast.success('Lesson completed', 'Great job, you can continue now.');

        this.courseService.getCourse(this.courseId()).subscribe({
          next: (updatedCourse) => {
            this.courseUpdated.emit(updatedCourse);
            this.courseService.loadCourses();
          },
        });
      },
      error: (err) => {
        this.submittingLessonTask.set(false);
        const message = err?.error?.message ?? 'Failed to submit task answer.';
        this.lessonTaskError.set(message);
      },
    });
  }

  tryAgainGenerate(): void {
    const lesson = this.selectedLesson();
    if (!lesson) return;
    this.lessonGenerating.set(true);
    this.lessonGenerationError.set(null);
    this.generateLesson(lesson.id);
  }

  private generateLesson(lessonId: string): void {
    this.courseService.generateLesson(this.courseId(), lessonId).subscribe({
      next: (result) => {
        if (!result || result.status === 'Error' || (!result.contentMarkdown && !result.task)) {
          this.lessonGenerationError.set('Something went wrong while generating the lesson. Please try again.');
          this.lessonGenerating.set(false);
          return;
        }

        this.lessonGenerationError.set(null);
        this.selectedLesson.update((value) =>
          value && value.id === lessonId
            ? {
                ...value,
                status: result.status,
                contentMarkdown: result.contentMarkdown,
              }
            : value
        );

        if (result.task) {
          this.lessonTask.set(result.task);
          if (this.selectedLesson()?.status === LessonStatus.Completed && result.task.completedCorrectOption) {
            this.lessonTaskSelection.set(result.task.completedCorrectOption as QuestionOption);
            this.lessonTaskMessage.set('Correct answer. Lesson completed.');
          }
        } else {
          const placeholder: LessonTask = {
            lessonId: lessonId,
            questionText: `What is the main takeaway from this lesson (${this.selectedLesson()?.title ?? ''})?`,
            optionA: 'To understand the core principles',
            optionB: 'To ignore the practical application',
            optionC: 'To focus on unrelated details',
            optionD: 'To skip the theoretical foundation',
            helpMarkdown: 'Review the lesson content and try to identify the primary objective. Use examples to validate your understanding.',
            completedCorrectOption: null,
          };
          this.lessonTask.set(placeholder);
        }
        this.lessonGenerating.set(false);
        this.toast.success('Lesson generated', 'Lesson content is ready.');

        // Refresh parent course layout state since status changed to Generated
        this.courseService.getCourse(this.courseId()).subscribe({
          next: (updatedCourse) => {
            this.courseUpdated.emit(updatedCourse);
          }
        });
      },
      error: () => {
        this.lessonGenerating.set(false);
        this.lessonGenerationError.set('Something went wrong while generating the lesson. Please try again.');
      },
    });
  }

  // --- NAVIGATION LOGIC ---

  private sortedModules(): RoadmapModule[] {
    return this.modules().slice().sort((a, b) => a.orderIndex - b.orderIndex);
  }

  private sortedLessons(module: RoadmapModule): RoadmapLesson[] {
    return module.lessons.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  }

  private sortedExams(module: RoadmapModule): RoadmapExam[] {
    return module.exams.slice().sort((a, b) => {
      if (a.isFinal !== b.isFinal) {
        return a.isFinal ? 1 : -1;
      }
      return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
    });
  }

  private moduleAllLessonsCompleted(module: RoadmapModule): boolean {
    return module.lessons.length > 0 && module.lessons.every((lesson) => lesson.status === LessonStatus.Completed);
  }

  private isExamLocked(exam: RoadmapExam, module: RoadmapModule): boolean {
    if (!exam.isFinal) {
      return !this.moduleAllLessonsCompleted(module);
    }

    const allLessonsCompleted = this.modules().every((item) =>
      item.lessons.every((lesson) => lesson.status === LessonStatus.Completed)
    );
    const nonFinalExamsCompleted = this.modules().every((item) =>
      item.exams.filter((e) => !e.isFinal).every((e) => e.status === 'Completed')
    );

    return !(allLessonsCompleted && nonFinalExamsCompleted);
  }

  openNextLesson(): void {
    const currentLesson = this.selectedLesson();
    if (!currentLesson) return;

    const currentModule = this.modules().find((module) =>
      module.lessons.some((lesson) => lesson.id === currentLesson.id)
    );

    if (currentModule) {
      const moduleLessons = this.sortedLessons(currentModule);
      const moduleLessonIndex = moduleLessons.findIndex((lesson) => lesson.id === currentLesson.id);
      const isLastLessonInModule = moduleLessonIndex >= 0 && moduleLessonIndex === moduleLessons.length - 1;

      if (isLastLessonInModule && currentLesson.status === LessonStatus.Completed) {
        const moduleExam = this.sortedExams(currentModule).find(
          (exam) => !exam.isFinal && exam.status !== 'Completed' && !this.isExamLocked(exam, currentModule)
        );

        if (moduleExam) {
          this.visible.set(false);
          this.openExamEvent.emit({ exam: moduleExam, module: currentModule });
          return;
        }
      }
    }

    const orderedLessons = this.sortedModules().flatMap((module) => this.sortedLessons(module));
    const currentIndex = orderedLessons.findIndex((lesson) => lesson.id === currentLesson.id);
    if (currentIndex < 0) return;

    const nextLesson = orderedLessons[currentIndex + 1];
    if (!nextLesson) return;

    this.selectedLesson.set(nextLesson);
  }

  openPreviousLesson(): void {
    const currentLesson = this.selectedLesson();
    if (!currentLesson) return;

    const orderedLessons = this.sortedModules().flatMap((module) => this.sortedLessons(module));
    const currentIndex = orderedLessons.findIndex((lesson) => lesson.id === currentLesson.id);
    if (currentIndex <= 0) return;

    const previousLesson = orderedLessons[currentIndex - 1];
    if (!previousLesson) return;

    this.selectedLesson.set(previousLesson);
  }

  nextLessonButtonLabel(): string {
    const currentLesson = this.selectedLesson();
    if (!currentLesson) return 'Next lesson';

    const currentModule = this.modules().find((module) =>
      module.lessons.some((lesson) => lesson.id === currentLesson.id)
    );
    if (!currentModule) return 'Next lesson';

    const moduleLessons = this.sortedLessons(currentModule);
    const moduleLessonIndex = moduleLessons.findIndex((lesson) => lesson.id === currentLesson.id);
    const isLastLessonInModule = moduleLessonIndex >= 0 && moduleLessonIndex === moduleLessons.length - 1;
    if (!isLastLessonInModule || currentLesson.status !== LessonStatus.Completed) return 'Next lesson';

    const hasOpenModuleExam = this.sortedExams(currentModule).some(
      (exam) => !exam.isFinal && exam.status !== 'Completed' && !this.isExamLocked(exam, currentModule)
    );

    return hasOpenModuleExam ? 'Go to exam' : 'Next lesson';
  }

  canOpenNextLessonAction(): boolean {
    const currentLesson = this.selectedLesson();
    if (!currentLesson) return false;

    const orderedLessons = this.sortedModules().flatMap((module) => this.sortedLessons(module));
    const currentIndex = orderedLessons.findIndex((lesson) => lesson.id === currentLesson.id);
    const hasNextLesson = currentIndex >= 0 && currentIndex < orderedLessons.length - 1;
    if (hasNextLesson) return true;

    const currentModule = this.modules().find((module) =>
      module.lessons.some((lesson) => lesson.id === currentLesson.id)
    );
    if (!currentModule) return false;

    const moduleLessons = this.sortedLessons(currentModule);
    const moduleLessonIndex = moduleLessons.findIndex((lesson) => lesson.id === currentLesson.id);
    const isLastLessonInModule = moduleLessonIndex >= 0 && moduleLessonIndex === moduleLessons.length - 1;
    if (!isLastLessonInModule || currentLesson.status !== LessonStatus.Completed) return false;

    return this.sortedExams(currentModule).some(
      (exam) => !exam.isFinal && exam.status !== 'Completed' && !this.isExamLocked(exam, currentModule)
    );
  }
}
