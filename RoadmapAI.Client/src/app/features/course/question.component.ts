import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

export type QuestionOption = 'A' | 'B' | 'C' | 'D';

@Component({
  selector: 'app-question',
  standalone: true,
  templateUrl: './question.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestionComponent {
  questionText = input.required<string>();
  optionA = input.required<string>();
  optionB = input.required<string>();
  optionC = input.required<string>();
  optionD = input.required<string>();

  orderIndex = input<number>();
  isAiGenerated = input<boolean>(false);
  disabled = input<boolean>(false);

  selectedOption = input<QuestionOption | null>(null);

  optionSelected = output<QuestionOption>();

  selectOption(option: QuestionOption): void {
    if (this.disabled()) return;
    this.optionSelected.emit(option);
  }

  optionClass(option: QuestionOption): string {
    const selected = this.selectedOption() === option;
    const disabled = this.disabled();

    let styles = 'w-full flex items-center gap-3 p-3.5 rounded-xl border transition-all text-left cursor-pointer focus:outline-none group ';

    if (selected) {
      styles += 'bg-primary border-primary text-white dark:text-on-primary font-semibold shadow-md';
    } else {
      if (disabled) {
        styles += 'bg-background/60 border-outline-variant opacity-60 cursor-not-allowed text-on-surface-variant';
      } else {
        styles += 'bg-background border-outline-variant hover:bg-surface-container-low hover:border-outline text-on-surface';
      }
    }

    return styles;
  }
}
