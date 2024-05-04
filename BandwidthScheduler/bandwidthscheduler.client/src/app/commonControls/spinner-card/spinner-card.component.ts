import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-spinner-card',
  templateUrl: './spinner-card.component.html',
  styleUrl: './spinner-card.component.scss',
})
export class SpinnerCardComponent {
  @Input() spinnerActive: boolean = false;
  @Input() stretch: SpinnerCardHorizontalStretch =
    SpinnerCardHorizontalStretch.Shrink;
}

export enum SpinnerCardHorizontalStretch {
  Shrink = 0,
  Grow = 1,
}
