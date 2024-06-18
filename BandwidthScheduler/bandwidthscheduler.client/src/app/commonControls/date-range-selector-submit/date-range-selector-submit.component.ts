import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
} from '@angular/core';
import { IDateRangeSelectorModel } from '../date-range-selector/IDateRangeSelectorModel';

@Component({
  selector: 'app-date-range-selector-submit',
  templateUrl: './date-range-selector-submit.component.html',
  styleUrl: './date-range-selector-submit.component.scss',
})
export class DateRangeSelectorSubmitComponent implements OnChanges {
  @Input() public Model: IDateRangeSelectorModel | undefined;
  @Output() public ModelChange = new EventEmitter<IDateRangeSelectorModel>();

  @Output() public ModelSubmit = new EventEmitter<IDateRangeSelectorModel>();

  ngOnChanges(changes: SimpleChanges): void {
    this.ModelChange.emit(this.Model);
  }

  public OnSubmit(): void {
    this.ModelChange.emit(this.Model);
    this.ModelSubmit.emit(this.Model);
  }

  public Validate(): { [key: string]: any } | null {
    const errors: { [key: string]: any } = {};

    if (!this.Model) {
      errors['ModelIsUndefined'] = true;
    }

    if (Object.keys(errors).length !== 0) return errors;

    if (this.Model!.start >= this.Model!.end) {
      errors['StartMustBeBefore'] = true;
    }

    return Object.keys(errors).length !== 0 ? errors : null;
  }
}
