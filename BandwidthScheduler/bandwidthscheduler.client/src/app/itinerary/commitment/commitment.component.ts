import { Component } from '@angular/core';
import { DateTimeRangeSelectorModel } from '../../commonControls/date-time-range-selector/date-time-range-selector-model';

@Component({
  selector: 'app-commitment',
  templateUrl: './commitment.component.html',
  styleUrl: './commitment.component.scss',
})
export class CommitmentComponent {
  public loading: boolean = false;

  public SelectedDateRange(range: DateTimeRangeSelectorModel): void {
    console.log(range);
  }
}
