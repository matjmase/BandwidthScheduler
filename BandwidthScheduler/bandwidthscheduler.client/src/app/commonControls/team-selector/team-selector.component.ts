import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  output,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { ITeam } from '../../models/db/ITeam';
import { BackendConnectService } from '../../services/backend-connect.service';
import { SpinnerCardHorizontalStretch } from '../spinner-card/spinner-card.component';
import { TeamSelectorType } from './team-selector-type';

@Component({
  selector: 'app-team-selector',
  templateUrl: './team-selector.component.html',
  styleUrl: './team-selector.component.scss',
})
export class TeamSelectorComponent implements OnInit {
  autoCompleteControl = new FormControl('');
  options: { [key: string]: ITeam } = {};
  filteredOptions: string[] = [];

  @Output() TeamSelected: EventEmitter<ITeam> = new EventEmitter<ITeam>();

  @Input() TeamType: TeamSelectorType | undefined;
  @Input() CardTitle: string = 'Select Team';
  @Input() SelectionTerm: string = 'Select';
  @Input() SpinnerActive: boolean = false;
  @Input() HorizontalStretch: SpinnerCardHorizontalStretch =
    SpinnerCardHorizontalStretch.Grow;

  constructor(private backend: BackendConnectService) {}

  ngOnInit(): void {
    if (this.TeamType === undefined) {
      throw new Error('Please configure the component.');
    }

    this.autoCompleteControl.valueChanges.subscribe({
      next: (value) => this.Filter(value ?? ''),
    });

    this.GetTeamsAutoComplete();
  }

  public SelectTeamSubmit(): void {
    const teamName = this.autoCompleteControl.value ?? '';
    const team = this.options[teamName];

    this.TeamSelected.emit(team);
  }

  public ResetForm(): void {
    this.autoCompleteControl.reset();
  }

  private Filter(value: string): void {
    this.filteredOptions = Object.keys(this.options).filter((option) =>
      this.FormatString(option).includes(this.FormatString(value))
    );
  }

  public GetTeamsAutoComplete(): void {
    switch (this.TeamType) {
      case TeamSelectorType.All: {
        this.backend.Team.GetAllTeams().subscribe({
          next: (teams) => {
            this.options = {};
            teams.forEach((element) => {
              this.options[element.name] = element;
            });
            this.Filter(this.autoCompleteControl.value ?? '');
          },
        });
        break;
      }
      case TeamSelectorType.Active: {
        this.backend.Team.GetActiveTeams().subscribe({
          next: (teams) => {
            this.options = {};
            teams.forEach((element) => {
              this.options[element.name] = element;
            });
            this.Filter(this.autoCompleteControl.value ?? '');
          },
        });
        break;
      }
      default:
        throw new Error('Not implemented team type');
    }
  }

  private FormatString(inputStr: string): string {
    return inputStr.trim().toLowerCase();
  }
}
