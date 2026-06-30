import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, InputText, Password, Button, Message],
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    const values = this.form.getRawValue();
    this.auth.register(values).subscribe({
      next: () => {
        // Auto-login after successful registration
        this.auth.login({ email: values.email, password: values.password }).subscribe({
          next: (res) => {
            this.auth.handleLoginSuccess(res);
            this.loading.set(false);
          },
          error: () => {
            this.router.navigate(['/login']);
            this.loading.set(false);
          }
        });
      },
      error: (err: { error: string[] | string }) => {
        const messages = err.error;
        this.error.set(Array.isArray(messages) ? messages.join('. ') : 'Registration failed');
        this.loading.set(false);
      }
    });
  }
}
