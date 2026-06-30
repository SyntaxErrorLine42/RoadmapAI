import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Toast],
  templateUrl: './app.html'
})
export class App {
  // Inject ThemeService here so it's constructed with the root component.
  private readonly theme = inject(ThemeService);
}
