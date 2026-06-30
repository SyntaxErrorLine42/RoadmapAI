import {
  Component, ChangeDetectionStrategy, inject, signal, output,
  ViewChild, ElementRef
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpEventType } from '@angular/common/http';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { ProgressBar } from 'primeng/progressbar';
import { CourseService } from '../core/services/course.service';
import { ToastService } from '../core/services/toast.service';

const ACCEPTED = '.pdf,.docx,.pptx,.txt,.md';

// Icon per file extension
function fileIcon(name: string): string {
  const ext = name.split('.').pop()?.toLowerCase() ?? '';
  if (['pdf'].includes(ext)) return 'pi pi-file-pdf';
  if (['docx'].includes(ext)) return 'pi pi-file-word';
  if (['pptx'].includes(ext)) return 'pi pi-file';
  return 'pi pi-file';
}

@Component({
  selector: 'app-create-course-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Button, InputText, ProgressBar, FormsModule],
  templateUrl: './create-course-dialog.component.html'
})
export class CreateCourseDialogComponent {
  private courseService = inject(CourseService);
  private toast = inject(ToastService);

  readonly accepted = ACCEPTED;
  readonly fileIcon = fileIcon;

  visible = signal(false);
  submitting = signal(false);
  dragging = signal(false);
  error = signal('');
  uploadPercent = signal(0);
  courseName = '';
  selectedFiles = signal<File[]>([]);

  created = output<{ id: string; name: string }>();

  open() {
    this.courseName = '';
    this.selectedFiles.set([]);
    this.error.set('');
    this.uploadPercent.set(0);
    this.submitting.set(false);
    this.visible.set(true);
  }

  onInputChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
    input.value = ''; // allow re-selecting same file
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragging.set(false);
    const files = Array.from(event.dataTransfer?.files ?? []);
    this.addFiles(files);
  }

  private addFiles(incoming: File[]) {
    const existing = this.selectedFiles();
    const existingNames = new Set(existing.map(f => f.name));
    // Deduplicate by name
    const merged = [...existing, ...incoming.filter(f => !existingNames.has(f.name))];
    this.selectedFiles.set(merged);
  }

  removeFile(index: number) {
    this.selectedFiles.update(files => files.filter((_, i) => i !== index));
  }

  clearFiles() {
    this.selectedFiles.set([]);
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  }

  totalSize(): string {
    return this.formatFileSize(this.selectedFiles().reduce((s, f) => s + f.size, 0));
  }

  submit() {
    if (!this.courseName.trim() || this.selectedFiles().length === 0) return;

    this.submitting.set(true);
    this.uploadPercent.set(0);
    this.error.set('');

    this.courseService.createCourse(this.courseName.trim(), this.selectedFiles()).subscribe({
      next: (event: any) => {
        const pct = CourseService.uploadPercent(event);
        if (pct !== null) {
          this.uploadPercent.set(pct);
          return;
        }
        // HttpEventType.Response - upload done
        if (event.type === HttpEventType.Response) {
          this.uploadPercent.set(100);
          this.submitting.set(false);
          this.visible.set(false);
          this.toast.success('Course created', 'Your course and files were uploaded successfully.');
          this.created.emit(event.body);
        }
      },
      error: (err: any) => {
        this.submitting.set(false);
        const message = err.error?.message || 'Failed to create course. Please try again.';
        this.error.set(message);
      },
    });
  }
}
