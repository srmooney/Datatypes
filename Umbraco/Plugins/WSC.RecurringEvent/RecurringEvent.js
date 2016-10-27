$.fn.extend({
	WSCRecurringEvent: function(options){
		var container = $(this);
		console.log(container);
		if (container.data('WSCRecurringEvent')){ return; }
		
		var $dateTable = null;
		var $datesInput = null;
		var $startDate = null;
		var $endDate = null;
		var $parameters = null;
		var $calendar = null;
		
		init();
				
		/* Methods */
		function init(){
			$parameters = $('div[data-parameter]', container).hide();
			$datesInput = $('input[name*="hdnExceptions"]', container);
			$startDate = container.parents('.tabpageContent').find('.propertyItem:has(.propertyItemheader:contains("Start Date")) .umbDateTimePicker input');
			if ($startDate){
				$startDate.on('change', function(e){
					$calendar.datepicker('option', 'minDate', $startDate.val().split(' ')[0]);
				});
			}
			$endDate = container.parents('.tabpageContent').find('.propertyItem:has(.propertyItemheader:contains("End Date")) .umbDateTimePicker input');
			if ($endDate){
				$endDate.on('change', function(e){
					$calendar.datepicker('option', 'minDate', $endDate.val().split(' ')[0]);
				});
			}
			
			$('select[id*="ddlType"]', container).on('change', function(e, dontClear){
				var val = $(this).val().toLowerCase();
				console.log('change', val);
				if (!dontClear) {
					ClearParameters();
				}
				$parameters.hide().filter('[data-parameter*="'+ val +'"]').show();
			}).trigger('change', false);	
			
			BuildTable();
			
			$calendar = $('.datepicker', container).datepicker({ 
				dateFormat: 'yy-mm-dd',
				minDate: ($startDate) ? $startDate.val().split(' ')[0] : null,
				maxDate: ($endDate) ? $endDate.val().split(' ')[0] : null,
				showOtherMonths: true,
				selectOtherMonths: true,
				hideIfNoPrevNext: true,
				onSelect: function(selectedDate, inst) {
					//console.log(inst);
					AddDate(selectedDate);
					//$calendar.datepicker('setDate', null);
				},
				beforeShowDay: function(date) {
					var instance = $(this).data('datepicker');
					var dateFormatted = $.datepicker.formatDate(instance.settings.dateFormat || $.datepicker._defaults.dateFormat, date, instance.settings);
					var css = '';
					var tip = '';
					var selectable = true;
					
					/* Holiday 
					var holiday = IsHoliday(date);
					if (holiday){
						css += ' holiday';
						tip = holiday +'\n';
					}
					*/
					
					/* Selected */
					if ($datesInput.val().indexOf(dateFormatted) >= 0){
						css += ' selected';
						tip = 'Closed\n';
						//selectable = false;
					}
					
					return [selectable, css, tip];
				}
			});			
		}
		
		function ClearParameters(){
			$parameters.find(':input').not(':button, :submit, :reset, :hidden, :radio').val('').removeAttr('checked').removeAttr('selected');
		}
		
		function RemoveDate(e) {
			e.preventDefault();
            $(this).parent().parent().remove();
            var dates = [];
            $dateTable.find('tr').each(function() {
                dates.push($(this).find('td:first').html());
            });
            $datesInput.val(dates.join(','));
			$calendar.datepicker('refresh');
        }

        function AddDate(d) {
            var dates = $datesInput.val();
            if (dates.indexOf(d) < 0) {
                var dateArray = (dates.length === 0) ? [] : dates.split(',');
                dateArray.push(d);
                dateArray.sort();
                $datesInput.val(dateArray.join(','));
                BuildTable();
            }
			else {
				$dateTable.find('tr:has(td:contains("'+ d +'")) a').click();
			}
            return false;
        }

		
		function BuildTable() {
            if (!$dateTable) {
                $dateTable = $('<table />');
                $datesInput.before($dateTable);
				$dateTable.on('click', 'a', RemoveDate);
            }

            $dateTable.empty();

            if ($datesInput.val() !== '') {
                var dates = $datesInput.val().split(',');
                for (var x = 0; x < dates.length; x++) {
                    $dateTable.append($('<tr><td>' + dates[x] + '</td><td><a href="#">Remove</a></td></tr>'));
                }
            }
        }
		
		function IsHoliday(dt_date) {
			// check simple dates (month/date - no leading zeroes)
			var n_date = dt_date.getDate(),
				n_month = dt_date.getMonth() + 1;
			var s_date1 = n_month + '/' + n_date;
		
			if (s_date1 == '1/1') { return 'New Year\'s Day'; } // New Year's Day
			//if (s_date1 == '6/14') { return 'Flag Day'; }  // Flag Day
			if (s_date1 == '7/4') { return 'Independence Day'; }  // Independence Day
			//if(s_date1 == '11/11') { return 'Veterans Day'; } // Veterans Day
			if (s_date1 == '12/25') { return 'Christmas Day'; } // Christmas Day
		
			// weekday from beginning of the month (month/num/day)
			var n_wday = dt_date.getDay(),
				n_wnum = Math.floor((n_date - 1) / 7) + 1;
			var s_date2 = n_month + '/' + n_wnum + '/' + n_wday;
		
			if (s_date2 == '1/3/1') { return 'Martin Luther King Day'; }  // Birthday of Martin Luther King, third Monday in January
			if (s_date2 == '2/3/1') { return 'Washington\'s Birthday'; } // Washington's Birthday, third Monday in February
			if (s_date2 == '9/1/1') { return 'Labor Day'; }  // Labor Day, first Monday in September
			if (s_date2 == '10/2/1') { return 'Columbus Day'; } // Columbus Day, second Monday in October
			if (s_date2 == '11/4/4') { return 'Thanksgiving Day'; } // Thanksgiving Day, fourth Thursday in November
			
		
			// weekday number from end of the month (month/num/day)
			var dt_temp = new Date (dt_date);
			dt_temp.setDate(1);
			dt_temp.setMonth(dt_temp.getMonth() + 1);
			dt_temp.setDate(dt_temp.getDate() - 1);
			n_wnum = Math.floor((dt_temp.getDate() - n_date - 1) / 7) + 1;
			var s_date3 = n_month + '/' + n_wnum + '/' + n_wday;
			
			// misc complex dates
			// Inauguration Day, January 20th every four years, starting in 1937. 
			if (s_date1 == '1/20' && (((dt_date.getFullYear() - 1937) % 4) == 0)) return 'Inauguration Day';
		
			// Memorial Day, last Monday in May
			if (n_month == 5 && n_date > 24 && n_wday == 1) return 'Memorial Day';
		
			// Election Day, Tuesday on or after November 2. 
			if (n_month == 11 && n_date >= 2 && n_date < 9 && n_wday == 2) return 'Election Day';
		
			return false;
		}
	
	
	}
});