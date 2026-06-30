import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly messageService = inject(MessageService);

  success(summary: string, detail?: string): void {
    this.messageService.add({
      severity: 'success',
      summary,
      detail,
      life: 3500,
    });
  }

  error(summary: string, detail?: string): void {
    this.messageService.add({
      severity: 'error',
      summary,
      detail,
      life: 3500,
    });
  }
}