import { HttpClient, HttpEventType, HttpUploadProgressEvent } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  CourseSummary,
  CourseDetail,
  CourseMaterial,
  CourseRoadmap,
  RenameCoursePayload,
  GenerateRoadmapResponse,
  LessonGeneratedResponse,
  LessonTask,
  SubmitLessonTaskRequest,
  SubmitLessonTaskResult,
  SubmitExamRequest,
  ExamSubmissionResult,
  ExamAttemptSummary,
  ExamAttemptDetail,
  RoadmapExamQuestion,
  CourseReviewSummary,
  CourseReviewDetail,
} from '../models/course.model';

@Injectable({ providedIn: 'root' })
export class CourseService {
  private http = inject(HttpClient);
  private api = `${environment.apiUrl}/courses`;

  courses = signal<CourseSummary[]>([]);
  loading = signal(false);

  loadCourses() {
    this.loading.set(true);
    this.http.get<CourseSummary[]>(this.api).subscribe({
      next: (data) => {
        this.courses.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  createCourse(name: string, files: File[]) {
    const formData = new FormData();
    formData.append('name', name);
    files.forEach((f) => formData.append('files', f));

    // reportProgress gives us UploadProgress events so we can show real %
    return this.http.post<{ id: string; name: string }>(this.api, formData, {
      reportProgress: true,
      observe: 'events',
    });
  }

  addMaterials(courseId: string, files: File[]) {
    const formData = new FormData();
    files.forEach((f) => formData.append('files', f));

    return this.http.post<CourseMaterial[]>(`${this.api}/${courseId}/materials`, formData, {
      reportProgress: true,
      observe: 'events',
    });
  }

  // Helper: extract upload % (0-99) from a progress event
  static uploadPercent(event: { type: number; loaded?: number; total?: number }): number | null {
    if (event.type === HttpEventType.UploadProgress) {
      const e = event as HttpUploadProgressEvent;
      return e.total ? Math.round((e.loaded / e.total) * 99) : null;
    }
    return null;
  }

  getCourse(id: string) {
    return this.http.get<CourseDetail>(`${this.api}/${id}`);
  }

  renameCourse(id: string, payload: RenameCoursePayload) {
    return this.http.patch<CourseSummary>(`${this.api}/${id}`, payload);
  }

  startIngestion(id: string) {
    return this.http.post<CourseDetail>(`${this.api}/${id}/ingest/start`, {});
  }

  generateRoadmap(id: string) {
    return this.http.post<GenerateRoadmapResponse>(`${this.api}/${id}/roadmap/generate`, {});
  }

  resetCourse(id: string) {
    return this.http.post<CourseDetail>(`${this.api}/${id}/reset`, {});
  }

  getRoadmap(id: string) {
    return this.http.get<CourseRoadmap>(`${this.api}/${id}/roadmap`);
  }

  generateLesson(courseId: string, lessonId: string) {
    return this.http.post<LessonGeneratedResponse>(`${this.api}/${courseId}/lessons/${lessonId}/generate`, {});
  }

  lessonStreamUrl(courseId: string, lessonId: string): string {
    return `${this.api}/${courseId}/lessons/${lessonId}/stream`;
  }

  completeLesson(courseId: string, lessonId: string) {
    return this.http.post(`${this.api}/${courseId}/lessons/${lessonId}/complete`, {});
  }

  getLessonTask(courseId: string, lessonId: string) {
    return this.http.get<LessonTask>(`${this.api}/${courseId}/lessons/${lessonId}/task`);
  }

  submitLessonTask(courseId: string, lessonId: string, payload: SubmitLessonTaskRequest) {
    return this.http.post<SubmitLessonTaskResult>(`${this.api}/${courseId}/lessons/${lessonId}/task/submit`, payload);
  }

  submitExam(courseId: string, examId: string, payload: SubmitExamRequest) {
    return this.http.post<ExamSubmissionResult>(`${this.api}/${courseId}/exams/${examId}/submit`, payload);
  }

  getExamQuestions(courseId: string, examId: string) {
    return this.http.get<RoadmapExamQuestion[]>(`${this.api}/${courseId}/exams/${examId}/questions`);
  }

  getExamAttempts(courseId: string) {
    return this.http.get<ExamAttemptSummary[]>(`${this.api}/${courseId}/exams/attempts`);
  }

  getExamAttemptDetail(courseId: string, attemptId: string) {
    return this.http.get<ExamAttemptDetail>(`${this.api}/${courseId}/exams/attempts/${attemptId}`);
  }

  generateReview(courseId: string) {
    return this.http.post<CourseReviewDetail>(`${this.api}/${courseId}/reviews/generate`, {});
  }

  getCourseReviews(courseId: string) {
    return this.http.get<CourseReviewSummary[]>(`${this.api}/${courseId}/reviews`);
  }

  getCourseReview(courseId: string, reviewId: string) {
    return this.http.get<CourseReviewDetail>(`${this.api}/${courseId}/reviews/${reviewId}`);
  }

  updateCourseNameInStore(id: string, name: string) {
    this.courses.update((items) =>
      items.map((item) => (item.id === id ? { ...item, name } : item))
    );
  }

  deleteCourse(id: string) {
    return this.http.delete(`${this.api}/${id}`);
  }

  deleteMaterial(courseId: string, materialId: string) {
    return this.http.delete(`${this.api}/${courseId}/materials/${materialId}`);
  }
}
