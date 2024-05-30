import { Component, Input } from '@angular/core';
import { UserLegendModel } from './user-legend-model';

@Component({
  selector: 'app-user-legend',
  templateUrl: './user-legend.component.html',
  styleUrl: './user-legend.component.scss',
})
export class UserLegendComponent {
  @Input() Model: UserLegendModel[] = [];
}
