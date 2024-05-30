import { Component, Input } from '@angular/core';
import { IGridLegendReadOnlyModel } from './grid-legend-read-only-model';

@Component({
  selector: 'app-grid-legend-read-only',
  templateUrl: './grid-legend-read-only.component.html',
  styleUrl: './grid-legend-read-only.component.scss',
})
export class GridLegendReadOnlyComponent {
  @Input() Model: IGridLegendReadOnlyModel | undefined;
}
