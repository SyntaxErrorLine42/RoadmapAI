import { Component, ChangeDetectionStrategy, computed, inject, viewChild } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { CourseService } from '../../core/services/course.service';
import { CourseSummary } from '../../core/models/course.model';
import { CreateCourseDialogComponent } from '../../shared/create-course-dialog.component';

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CreateCourseDialogComponent, DatePipe],
  templateUrl: './home.component.html'
})
export class HomeComponent {
  protected readonly courseService = inject(CourseService);
  private readonly router = inject(Router);

  protected readonly coursesByPriority = computed(() =>
    [...this.courseService.courses()].sort((a, b) => {
      if (a.completionPercent !== b.completionPercent) {
        return a.completionPercent - b.completionPercent;
      }
      return a.name.localeCompare(b.name, undefined, { sensitivity: 'base' });
    })
  );

  private createDialog = viewChild(CreateCourseDialogComponent);

  protected readonly latestCourse = computed(() => {
    const courses = this.courseService.courses();
    return courses.length > 0 ? courses[0] : null;
  });

  protected readonly averageCompletion = computed(() => {
    const courses = this.courseService.courses();
    if (courses.length === 0) return 0;
    const sum = courses.reduce((acc, c) => acc + (c.completionPercent || 0), 0);
    return Math.round(sum / courses.length);
  });

  openCreateDialog() {
    this.createDialog()?.open();
  }

  getProgressBarColor(percent: number): string {
    const val = percent || 0;
    let r, g, b;
    if (val < 50) {
      const t = val / 50;
      r = Math.round(244 + (245 - 244) * t);
      g = Math.round(63 + (158 - 63) * t);
      b = Math.round(94 + (11 - 94) * t);
    } else {
      const t = (val - 50) / 50;
      r = Math.round(245 + (16 - 245) * t);
      g = Math.round(158 + (185 - 158) * t);
      b = Math.round(11 + (129 - 11) * t);
    }
    return `rgb(${r}, ${g}, ${b})`;
  }

  onCourseCreated(result: { id: string; name: string }) {
    this.courseService.loadCourses();
    this.router.navigate(['/courses', result.id]);
  }
}
