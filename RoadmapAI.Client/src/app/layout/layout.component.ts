import { Component, ChangeDetectionStrategy, computed, inject, signal, viewChild, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { Drawer } from 'primeng/drawer';
import { Button } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { AuthService } from '../core/services/auth.service';
import { ThemeService } from '../core/services/theme.service';
import { CourseService } from '../core/services/course.service';
import { CreateCourseDialogComponent } from '../shared/create-course-dialog.component';

@Component({
  selector: 'app-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    Drawer,
    Button,
    Tooltip,
    CreateCourseDialogComponent,
  ],
  templateUrl: './layout.component.html'
})
export class LayoutComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly courseService = inject(CourseService);
  private readonly router = inject(Router);

  protected readonly sortedCourses = computed(() =>
    [...this.courseService.courses()].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }))
  );

  readonly collapsed = signal(false);
  readonly profileMenuOpen = signal(false);

  private createDialog = viewChild(CreateCourseDialogComponent);

  ngOnInit() {
    this.courseService.loadCourses();
  }

  onAddCourse(): void {
    this.createDialog()?.open();
  }

  onCourseCreated(result: { id: string; name: string }): void {
    this.courseService.loadCourses();
    this.router.navigate(['/courses', result.id]);
  }
}
