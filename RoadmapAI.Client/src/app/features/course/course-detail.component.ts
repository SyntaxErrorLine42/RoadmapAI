import { Component, ChangeDetectionStrategy, inject, signal, computed, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SlicePipe } from '@angular/common';
import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { ProgressBar } from 'primeng/progressbar';
import { marked } from 'marked';
import { CourseService } from '../../core/services/course.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CourseDetail,
  CourseReviewDetail,
  CourseReviewSummary,
  CourseMaterial,
  CourseStatus,
  ExamAttemptDetail,
  ExamAttemptSummary,
  ExamSubmissionResult,
  LessonTask,
  LessonStatus,
  MaterialStatus,
  RoadmapExam,
  RoadmapExamQuestion,
  RoadmapModule,
  RoadmapLesson,
  SubmitLessonTaskResult,
} from '../../core/models/course.model';
import { LessonDialogComponent } from './lesson-dialog.component';
import { ExamDialogComponent } from './exam-dialog.component';

const ACCEPTED = '.pdf,.docx,.pptx,.txt,.md';

function fileIcon(name: string): string {
  const ext = name.split('.').pop()?.toLowerCase() ?? '';
  if (['pdf'].includes(ext)) return 'pi pi-file-pdf';
  if (['docx'].includes(ext)) return 'pi pi-file-word';
  if (['pptx'].includes(ext)) return 'pi pi-file';
  return 'pi pi-file';
}

type SelectedExamContext = {
  exam: RoadmapExam;
  moduleId: string;
  moduleTitle: string;
};

@Component({
  selector: 'app-course-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, InputText, ProgressBar, LessonDialogComponent, ExamDialogComponent],
  templateUrl: './course-detail.component.html'
})
export class CourseDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(CourseService);
  private toast = inject(ToastService);
  readonly fileIcon = fileIcon;
  readonly accepted = ACCEPTED;
  readonly courseStatus = CourseStatus;
  readonly lessonStatus = LessonStatus;
  readonly materialStatus = MaterialStatus;

  renderMarkdown(text: string): string {
    return marked.parse(text ?? '') as string;
  }

  completionColorClass(percent: number): string {
    if (percent < 30) return 'bg-red-500';
    if (percent < 70) return 'bg-amber-500';
    if (percent < 100) return 'bg-primary';
    return 'bg-emerald-500';
  }

  course = signal<CourseDetail | null>(null);
  loading = signal(true);

  uploadingFiles = signal(false);
  uploadPercent = signal(0);

  deleteDialogOpen = signal(false);
  deleting = signal(false);

  filesDialogOpen = signal(false);

  renameDialogOpen = signal(false);
  renaming = signal(false);
  renameName = signal('');
  renameError = signal('');

  ingesting = signal(false);
  roadmapGenerating = signal(false);
  roadmapError = signal<string | null>(null);
  summaryDialogOpen = signal(false);
  resetDialogOpen = signal(false);
  resettingCourse = signal(false);
  resetError = signal<string | null>(null);

  lessonDialogOpen = signal(false);
  selectedLesson = signal<RoadmapLesson | null>(null);

  examDialogOpen = signal(false);
  selectedExam = signal<SelectedExamContext | null>(null);
  viewAttemptId = signal<string | null>(null);
  examAttempts = signal<ExamAttemptSummary[]>([]);

  reviewGenerating = signal(false);
  reviewsDialogOpen = signal(false);
  selectedReview = signal<CourseReviewDetail | null>(null);
  courseReviews = signal<CourseReviewSummary[]>([]);

  readonly ingestPercent = computed(() => {
    const c = this.course();
    if (!c || c.materials.length === 0) return 0;
    const readyCount = c.materials.filter((m) => m.status === MaterialStatus.Ready).length;
    return Math.round((readyCount / c.materials.length) * 100);
  });

  readonly canGenerateRoadmap = computed(() => {
    const c = this.course();
    if (!c) return false;
    //TODO decide on roadmap regenreating
    return c.status === CourseStatus.RoadmapReady
      // || c.status === CourseStatus.Ready;
  });

  readonly roadmapStats = computed(() => {
    const c = this.course();
    const modules = c?.roadmap?.modules ?? [];

    const totalLessons = modules.reduce((sum, module) => sum + module.lessons.length, 0);
    const completedLessons = modules.reduce(
      (sum, module) => sum + module.lessons.filter((lesson) => lesson.status === LessonStatus.Completed).length,
      0
    );
    const totalExams = modules.reduce((sum, module) => sum + module.exams.length, 0);
    const completedExams = modules.reduce(
      (sum, module) => sum + module.exams.filter((exam) => exam.status === 'Completed').length,
      0
    );
    const progressPercent = totalLessons === 0 ? 0 : Math.round((completedLessons / totalLessons) * 100);

    const hasPendingModuleExam = modules.some(
      (module) => this.moduleAllLessonsCompleted(module) && module.exams.some((exam) => !exam.isFinal && exam.status !== 'Completed')
    );
    const examPenaltyPercent = totalLessons === 0 || !hasPendingModuleExam ? 0 : Math.round(100 / totalLessons);
    const completionPercent = Math.max(0, progressPercent - examPenaltyPercent);

    return {
      materials: c?.materials.length ?? 0,
      totalLessons,
      completedLessons,
      totalExams,
      completedExams,
      progressPercent,
      completionPercent,
    };
  });

  readonly nextLessonToStudy = computed(() => {
    const modules = this.course()?.roadmap?.modules ?? [];
    return this.findFirstIncompleteLesson(modules);
  });

  readonly hasPendingRequiredExam = computed(() => {
    const modules = (this.course()?.roadmap?.modules ?? []).slice().sort((a, b) => a.orderIndex - b.orderIndex);
    for (const module of modules) {
      if (!this.moduleAllLessonsCompleted(module)) {
        return false;
      }

      const nonFinalExams = module.exams.filter((exam) => !exam.isFinal);
      if (nonFinalExams.length > 0 && nonFinalExams.some((exam) => exam.status !== 'Completed')) {
        return true;
      }
    }

    return false;
  });

  readonly allExams = computed(() => {
    const modules = this.sortedModules();
    return modules
      .flatMap((module) =>
        this.sortedExams(module).map((exam) => ({
          exam,
          module,
        }))
      );
  });

  readonly finalExamContext = computed(() => {
    const modules = this.course()?.roadmap?.modules ?? [];
    for (const module of modules) {
      const finalExam = module.exams.find((exam) => exam.isFinal);
      if (finalExam) {
        return { exam: finalExam, module };
      }
    }
    return null;
  });

  readonly courseSummaryHtml = computed<string>(() => {
    return this.course()?.roadmap?.summaryMarkdown ?? '';
  });

  readonly selectedReviewHtml = computed<string>(() => {
    return this.selectedReview()?.contentMarkdown ?? '';
  });

  deleteMaterialDialogOpen = signal(false);
  deletingMaterial = signal(false);
  materialToDelete = signal<CourseMaterial | null>(null);

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (!id) {
        this.course.set(null);
        this.loading.set(false);
        return;
      }

      this.loadCourse(id);
    });
  }

  ngOnDestroy(): void {
    
  }

  startOrContinueCourse(): void {
    const lesson = this.nextLessonToStudy();
    if (!lesson) return;
    this.openLesson(lesson);
  }

  openSummaryDialog(): void {
    this.summaryDialogOpen.set(true);
  }

  openExam(exam: RoadmapExam, module: RoadmapModule): void {
    if (this.isExamLocked(exam, module)) return;

    if (exam.status === 'Completed') {
      const matchingAttempt = this.examAttempts()
        .slice()
        .sort((a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime())
        .find((a) => a.examId === exam.id);

      if (matchingAttempt) {
        this.viewExamAttempt(matchingAttempt.id);
        return;
      }
    }

    this.selectedExam.set({ exam, moduleId: module.id, moduleTitle: module.title });
    this.viewAttemptId.set(null);
    this.examDialogOpen.set(true);
  }

  retryExamAttempt(examId: string): void {
    const match = this.allExams().find((entry) => entry.exam.id === examId);
    if (!match) return;
    this.openExam(match.exam, match.module);
  }

  viewExamAttempt(attemptId: string): void {
    this.viewAttemptId.set(attemptId);
    this.examDialogOpen.set(true);
  }

  moduleCompletedLessons(module: RoadmapModule): number {
    return module.lessons.filter((lesson) => lesson.status === LessonStatus.Completed).length;
  }

  moduleProgressPercent(module: RoadmapModule): number {
    if (module.lessons.length === 0) return 0;
    return Math.round((this.moduleCompletedLessons(module) / module.lessons.length) * 100);
  }

  moduleAllLessonsCompleted(module: RoadmapModule): boolean {
    return module.lessons.length > 0 && module.lessons.every((lesson) => lesson.status === LessonStatus.Completed);
  }

  sortedModules(): RoadmapModule[] {
    return (this.course()?.roadmap?.modules ?? []).slice().sort((a, b) => a.orderIndex - b.orderIndex);
  }

  sortedLessons(module: RoadmapModule): RoadmapLesson[] {
    return module.lessons.slice().sort((a, b) => a.orderIndex - b.orderIndex);
  }

  sortedExams(module: RoadmapModule): RoadmapExam[] {
    return module.exams.filter((exam) => !exam.isFinal).slice().sort((a, b) => {
      return a.title.localeCompare(b.title, undefined, { sensitivity: 'base' });
    });
  }

  isModuleUnlocked(module: RoadmapModule): boolean {
    return true;
  }

  isLessonLocked(module: RoadmapModule): boolean {
    return false;
  }

  lessonLockReason(module: RoadmapModule): string {
    return '';
  }

  isExamLocked(exam: RoadmapExam, module: RoadmapModule): boolean {
    const c = this.course();
    const modules = c?.roadmap?.modules ?? [];
    const selectedModule = modules.find((item) => item.id === module.id);

    if (!selectedModule) return true;
    if (!exam.isFinal) {
      return !this.moduleAllLessonsCompleted(selectedModule);
    }

    const allLessonsCompleted = modules.every((item) => item.lessons.every((lesson) => lesson.status === LessonStatus.Completed));
    const nonFinalExamsCompleted = modules.every((item) =>
      item.exams.filter((itemExam) => !itemExam.isFinal).every((itemExam) => itemExam.status === 'Completed')
    );

    return !(allLessonsCompleted && nonFinalExamsCompleted);
  }

  examStateLabel(exam: RoadmapExam, module: RoadmapModule): string {
    if (exam.status === 'Completed') return 'Completed';
    return this.isExamLocked(exam, module) ? 'Locked' : 'Ready';
  }

  examLockReason(exam: RoadmapExam, module: RoadmapModule): string {
    if (!this.isExamLocked(exam, module)) return '';
    if (!exam.isFinal) {
      return 'Complete all lessons in this module first.';
    }

    return 'Complete all lessons and module exams before final exam.';
  }

  startButtonLabel(): string {
    if (this.nextLessonToStudy()) return 'Continue Course';
    if (this.hasPendingRequiredExam()) return 'Complete Module Exam';
    return 'Course Completed';
  }

  private loadCourse(id: string): void {
    this.loading.set(true);
    this.course.set(null);
    this.examAttempts.set([]);
    this.courseReviews.set([]);

    this.courseService.getCourse(id).subscribe({
      next: (c) => {
        this.course.set(c);
        this.loadExamAttempts(c.id);
        this.loadCourseReviews(c.id);
        this.autoStartIngestionIfNeeded(c);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  loadExamAttempts(courseId: string): void {
    this.courseService.getExamAttempts(courseId).subscribe({
      next: (attempts) => this.examAttempts.set(attempts),
      error: () => this.examAttempts.set([]),
    });
  }

  private loadCourseReviews(courseId: string): void {
    this.courseService.getCourseReviews(courseId).subscribe({
      next: (reviews) => this.courseReviews.set(reviews),
      error: () => this.courseReviews.set([]),
    });
  }

  examAttemptDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleString();
  }

  generateReview(): void {
    const c = this.course();
    if (!c) return;

    this.reviewGenerating.set(true);
    this.courseService.generateReview(c.id).subscribe({
      next: (review) => {
        this.reviewGenerating.set(false);
        this.selectedReview.set(review);
        this.reviewsDialogOpen.set(true);
        this.loadCourseReviews(c.id);
      },
      error: () => {
        this.reviewGenerating.set(false);
        this.toast.error('Error', 'Error has occured try again later.');
      },
    });
  }

  openReview(reviewId: string): void {
    const c = this.course();
    if (!c) return;

    this.courseService.getCourseReview(c.id, reviewId).subscribe({
      next: (review) => {
        this.selectedReview.set(review);
        this.reviewsDialogOpen.set(true);
      },
      error: () => {
        this.toast.error('Error', 'Error has occured try again later.');
      },
    });
  }

  onReviewDialogVisibleChange(visible: boolean): void {
    this.reviewsDialogOpen.set(visible);
    if (visible) return;
    this.selectedReview.set(null);
  }

  openLatestReview(): void {
    const latest = this.courseReviews()[0];
    if (!latest) {
      return;
    }

    this.openReview(latest.id);
  }

  private autoStartIngestionIfNeeded(course: CourseDetail): void {
    const hasPendingMaterials = course.materials.some(
      (m) => m.status === MaterialStatus.Uploaded || m.status === MaterialStatus.Ingesting
    );
    const shouldStart = hasPendingMaterials && !this.ingesting();
    if (!shouldStart) return;

    this.startIngestion();
  }

  onAddFilesChange(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0) return;
    const c = this.course();
    if (!c) return;

    this.uploadingFiles.set(true);
    this.uploadPercent.set(0);

    this.courseService.addMaterials(c.id, files).subscribe({
      next: (event: HttpEvent<CourseMaterial[]>) => {
        const pct = CourseService.uploadPercent(event);
        if (pct !== null) {
          this.uploadPercent.set(pct);
          return;
        }
        if (event instanceof HttpResponse) {
          const newMaterials = event.body ?? [];
          this.course.update(course => course ? {
            ...course,
            materials: [...course.materials, ...newMaterials]
          } : null);

          this.uploadingFiles.set(false);
          this.uploadPercent.set(100);
          this.filesDialogOpen.set(true);
          this.courseService.loadCourses(); // refresh counts sidebar
          this.toast.success('Files added', `${newMaterials.length} file(s) uploaded successfully.`);
          this.startIngestion();
        }
      },
      error: (err) => {
        this.uploadingFiles.set(false);
      }
    });
  }

  startIngestion(): void {
    const c = this.course();
    if (!c) return;

    this.ingesting.set(true);
    this.courseService.startIngestion(c.id).subscribe({
      next: (updated) => {
        this.course.set(updated);
        this.ingesting.set(false);
        this.courseService.loadCourses();
      },
      error: (err) => {
        this.ingesting.set(false);
      },
    });
  }

  generateRoadmap(): void {
    const c = this.course();
    if (!c) return;

    this.roadmapGenerating.set(true);
    this.roadmapError.set(null);

    this.courseService.generateRoadmap(c.id).subscribe({
      next: () => {
        this.courseService.getCourse(c.id).subscribe({
          next: (updated) => {
            this.course.set(updated);
            this.roadmapGenerating.set(false);
            this.courseService.loadCourses();
            this.toast.success('Roadmap generated', 'Your modules and lessons are ready.');
          },
          error: () => {
            this.roadmapGenerating.set(false);
            this.roadmapError.set('Roadmap generated, but refreshing failed.');
          },
        });
      },
      error: (err) => {
        this.roadmapGenerating.set(false);
        const message = err?.error?.message ?? 'Failed to generate roadmap.';
        this.roadmapError.set(message);
      },
    });
  }

  resetCourse(): void {
    const c = this.course();
    if (!c) return;

    this.resettingCourse.set(true);
    this.resetError.set(null);

    this.courseService.resetCourse(c.id).subscribe({
      next: (updated) => {
        this.course.set(updated);
        this.resettingCourse.set(false);
        this.resetDialogOpen.set(false);
        this.examAttempts.set([]);
        this.courseService.loadCourses();
        this.toast.success('Course reset', 'Progress was cleared and course files were kept.');
      },
      error: (err) => {
        this.resettingCourse.set(false);
        const message = err?.error?.message ?? 'Failed to reset course.';
        this.resetError.set(message);
      },
    });
  }

  openLesson(lesson: RoadmapLesson): void {
    const c = this.course();
    if (!c) return;

    const module = c.roadmap?.modules.find((item) => item.lessons.some((moduleLesson) => moduleLesson.id === lesson.id));
    if (module && this.isLessonLocked(module)) {
      return;
    }

    this.selectedLesson.set(lesson);
    this.lessonDialogOpen.set(true);
  }

  private findFirstIncompleteLesson(modules: RoadmapModule[]): RoadmapLesson | null {
    for (const module of modules.slice().sort((a, b) => a.orderIndex - b.orderIndex)) {
      if (!this.isModuleUnlocked(module)) {
        return null;
      }

      const lesson = module.lessons
        .slice()
        .sort((a, b) => a.orderIndex - b.orderIndex)
        .find((item) => item.status !== LessonStatus.Completed);

      if (lesson) {
        return lesson;
      }
    }

    return null;
  }

  materialStatusClass(status: MaterialStatus): string {
    switch (status) {
      case MaterialStatus.Ready:
        return 'text-emerald-600 dark:text-emerald-400';
      case MaterialStatus.Error:
        return 'text-red-500';
      case MaterialStatus.Ingesting:
        return 'text-amber-500';
      default:
        return 'text-surface-400';
    }
  }

  openRenameDialog(): void {
    const currentCourse = this.course();
    if (!currentCourse) return;

    this.renameName.set(currentCourse.name);
    this.renameError.set('');
    this.renameDialogOpen.set(true);
  }

  onRenameInput(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    this.renameName.set(target?.value ?? '');
  }

  closeRenameDialog(): void {
    this.renameDialogOpen.set(false);
    this.renameError.set('');
    this.renaming.set(false);
  }

  saveCourseName(): void {
    const currentCourse = this.course();
    if (!currentCourse) return;

    const name = this.renameName().trim();
    if (!name) {
      this.renameError.set('Course name is required.');
      return;
    }
    if (name.length > 120) {
      this.renameError.set('Course name must be 120 characters or fewer.');
      return;
    }

    this.renaming.set(true);
    this.renameError.set('');

    this.courseService.renameCourse(currentCourse.id, { name }).subscribe({
      next: (updatedCourse) => {
        this.course.update((value) => (value ? { ...value, name: updatedCourse.name } : null));
        this.courseService.updateCourseNameInStore(updatedCourse.id, updatedCourse.name);
        this.closeRenameDialog();
      },
      error: (err) => {
        this.renaming.set(false);
        this.renameError.set(err?.error?.message ?? 'Failed to rename course.');
      },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  }

  deleteCourse() {
    const c = this.course();
    if (!c) return;

    this.deleting.set(true);
    this.courseService.deleteCourse(c.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteDialogOpen.set(false);
        this.courseService.loadCourses();
        this.router.navigate(['/']);
        this.toast.success('Course deleted', 'The course was removed successfully.');
      },
      error: (err) => {
        this.deleting.set(false);
      },
    });
  }

  confirmDeleteMaterial(mat: CourseMaterial) {
    this.materialToDelete.set(mat);
    this.deleteMaterialDialogOpen.set(true);
  }

  deleteMaterial() {
    const c = this.course();
    const mat = this.materialToDelete();
    if (!c || !mat) return;

    this.deletingMaterial.set(true);
    this.courseService.deleteMaterial(c.id, mat.id).subscribe({
      next: () => {
        // Update local state by removing the material
        this.course.update(course => course ? {
          ...course,
          materials: course.materials.filter(m => m.id !== mat.id)
        } : null);
        this.deletingMaterial.set(false);
        this.deleteMaterialDialogOpen.set(false);
        this.materialToDelete.set(null);
        this.courseService.loadCourses();
        this.toast.success('File deleted', 'Your file has been deleted.');
      },
      error: (err) => {
        this.deletingMaterial.set(false);
      },
    });
  }
}
